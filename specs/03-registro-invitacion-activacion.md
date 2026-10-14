# SPEC 03 — Registro por invitación y activación

> **Estado:** Borrador
> **Dependencias:** SPEC 01, SPEC 02
> **Fecha:** 2026-10-13
> **Objetivo:** Permitir que un administrador registre usuarios y les envíe una invitación segura para establecer su contraseña.

## Alcance

**Incluye:**

- Casos de uso `RegisterUser`, `ActivateUser` y `ResendUserInvitation`.
- Puerto `IEmailSender` en Application.
- Adaptador HTTP de Resend en Infrastructure mediante `HttpClient`.
- Usuario nuevo con rol `User` y sin contraseña utilizable.
- Token de activación de un solo uso y vigencia configurable.
- Enlace hacia una URL de cliente configurable.
- Reenvío administrativo.
- Idempotencia y reintentos acotados del envío.

**Fuera de alcance:**

- SDK específico de Resend.
- SMTP o MailKit.
- UI de activación alojada por la API.
- Outbox transaccional.
- Registro anónimo.
- Recuperación ordinaria de contraseñas.

## Contratos HTTP

```text
POST /api/auth/register                  [AdminOnly]
Request:  { email }
Response: 201 { id, email, role: "User" }

POST /api/auth/activate                  [Anonymous]
Request:  { email, token, newPassword }
Response: 204

POST /api/auth/users/{id}/invitation     [AdminOnly]
Response: 204
```

Resultados relevantes:

- Email duplicado: 409.
- Usuario de reenvío inexistente: 404.
- Usuario ya activado: 409.
- Token inválido, expirado o utilizado: 400 genérico.
- Resend no disponible: 503 `email_delivery_unavailable`; el usuario permanece pendiente.

Configuración:

- `UserInvitation:ActivationBaseUrl`.
- `UserInvitation:TokenLifetimeHours`, predeterminado en 24.
- `Resend:BaseUrl`.
- `Resend:ApiKey`, siempre como secreto.
- `Resend:FromAddress`.
- `Resend:FromName`.

## Plan de implementación

1. Definir los casos de uso y `IEmailSender` en Application.
2. Crear usuarios pendientes con rol `User` y tokens de activación de Identity.
3. Implementar el cliente HTTP de Resend sin SDK específico.
4. Incorporar una `Idempotency-Key` estable durante los reintentos de cada intento.
5. Implementar registro, activación y reenvío.
6. Agregar plantillas seguras de correo y URL codificada.
7. Probar el flujo con un doble de `IEmailSender` y PostgreSQL efímero.

## Criterios de aceptación

- [ ] Solo `Admin` puede registrar o reenviar invitaciones.
- [ ] El alta recibe únicamente el email y asigna siempre el rol `User`.
- [ ] La respuesta y los logs nunca contienen el token de activación, API key ni contraseña.
- [ ] El usuario no puede iniciar sesión antes de establecer su contraseña.
- [ ] El enlace usa `ActivationBaseUrl` y un token codificado correctamente.
- [ ] El token vence después de 24 horas por defecto y queda invalidado tras utilizarse.
- [ ] Activación correcta devuelve 204.
- [ ] Tokens inválidos, vencidos o usados devuelven el mismo 400 seguro.
- [ ] Una falla de Resend devuelve 503 y conserva al usuario pendiente.
- [ ] El administrador puede reenviar la invitación de un usuario pendiente.
- [ ] Los reintentos técnicos usan la misma clave de idempotencia.
- [ ] Un reenvío manual crea una clave de idempotencia nueva.
- [ ] El dominio remitente de Resend se documenta como prerrequisito de despliegue.
- [ ] Las pruebas automatizadas no realizan llamadas reales a Resend.

## Decisiones y riesgos

- **Sí:** API HTTP de Resend mediante `HttpClient` e `IEmailSender`.
- **No:** dependencia directa del SDK de Resend o de MailKit.
- **Sí:** el usuario persiste como pendiente si falla el correo.
- **No:** intentar una transacción distribuida entre PostgreSQL y Resend.
- **Riesgo:** PostgreSQL puede confirmar el usuario y Resend fallar; se mitiga con estado pendiente y reenvío administrativo.
- **Riesgo:** las claves de idempotencia tienen vigencia limitada; cada reenvío manual representa un intento nuevo.

## Qué no está incluido en esta spec

- Registro público.
- SMTP, MailKit o proveedores específicos distintos del adaptador HTTP configurable.
- UI cliente de activación.
- Outbox y procesamiento asíncrono de correo.
- Operaciones del balance global.
