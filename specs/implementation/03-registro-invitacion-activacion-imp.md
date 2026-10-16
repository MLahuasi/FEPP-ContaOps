# Entrega para SPEC 04 — Invitaciones y activación

## Contratos disponibles

La capa Application expone estos puertos en
`Fepp.ContaOps.Application.Abstractions`:

| Contrato | Operación | Uso en SPEC 04 |
|---|---|---|
| `IUserInvitationService` | `RegisterPendingAsync(email)` | Registrar una cuenta pendiente. |
| `IUserInvitationService` | `ActivateAsync(userId, token, newPassword)` | Consumir la invitación y activar la cuenta. |
| `IUserInvitationService` | `ResendAsync(userId)` | Generar un reenvío administrativo. |
| `IInvitationNotifier` | `NotifyAsync(invitation)` | Entregar la notificación después del commit. |

`IEmailSender` es un puerto de Infrastructure/Application y no debe utilizarse
directamente desde controllers ni DTOs de Api.

## Resultado interno

`UserInvitationResult` contiene:

- `User` de Domain;
- `ActivationToken`.

El token es exclusivamente interno para construir el correo. No debe formar
parte de respuestas HTTP, errores, logs, trazas ni métricas.

## Orden obligatorio del registro y reenvío

1. Ejecutar `RegisterPendingAsync` o `ResendAsync`.
2. Cuando el resultado sea exitoso, invocar `IInvitationNotifier`.
3. Devolver una respuesta HTTP sin token ni enlace completo.

La implementación de Infrastructure confirma la transacción de Identity antes
de devolver el resultado. Si el notifier falla, la cuenta permanece pendiente y
SPEC 04 debe mapear la excepción a `email_delivery_unavailable` sin reintentar
automáticamente.

## Activación

El endpoint de SPEC 04 debe recibir por HTTPS el `userId`, el token extraído
del fragmento de la URL y la primera contraseña. Application localiza la
cuenta directamente por `UserId`, sin requerir el email y sin exponer detalles
sobre si el usuario, token o estado eran incorrectos.

Todos estos casos devuelven el mismo error neutral:

```text
invalid_activation_token
```

La activación aplica la política de contraseñas de Identity, confirma el email,
marca la cuenta como `Active`, actualiza el security stamp y deja inutilizable
el token consumido.

## Errores de entrega

`EmailDeliveryUnavailableException.ErrorCode` contiene:

```text
email_delivery_unavailable
```

La capa HTTP debe producir el ProblemDetails definido por sus contratos sin
incluir el mensaje interno, el destinatario, el cuerpo, el token, el enlace o
las credenciales SMTP.

## Servicios registrados

El composition root registra:

- `IUserInvitationService` → `IdentityUserInvitationService`;
- `IInvitationNotifier` → `InvitationEmailNotifier`;
- `IEmailSender` → `MailKitSmtpEmailSender`.

MailKit permanece confinado a Infrastructure. SPEC 04 no debe referenciar
MailKit, MimeKit ni `ApplicationIdentityUser`.
