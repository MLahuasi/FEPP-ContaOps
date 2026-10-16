# SPEC 03 — Núcleo de invitaciones y correo SMTP

> **Estado:** Aprobado
> **Dependencias:** SPEC 01, SPEC 02
> **Fecha:** 2026-10-16
> **Objetivo:** Definir el ciclo seguro de invitación y activación de usuarios junto con un puerto de correo SMTP reemplazable e inicialmente compatible con Gmail mediante MailKit.

## Por qué existe esta spec

Esta spec separa el ciclo de vida de una invitación de los detalles HTTP y del proveedor de correo. Gmail será el primer proveedor operativo, pero Application no dependerá de Gmail ni de MailKit. La API HTTP y el cierre de los pendientes de identidad se especifican en SPEC 04.

## Alcance

**Incluye:**

- Estados funcionales `PendingActivation` y `Active`.
- Registro administrativo de una cuenta pendiente.
- Generación, validación, consumo e invalidación de invitaciones.
- Establecimiento de la primera contraseña mediante Identity.
- Persistencia del usuario antes de intentar el envío de correo.
- Puertos neutrales `IUserInvitationService`, `IInvitationNotifier` e `IEmailSender`.
- Transporte SMTP reemplazable mediante una configuración neutral `Email:Smtp`.
- Adaptador inicial con MailKit y Gmail SMTP.
- Plantillas HTML y texto plano versionadas y parametrizables.
- Enlaces de activación seguros con `userId` y token en el fragmento de la URL.
- Persistencia configurable del key ring de Data Protection.
- Actualización coordinada de `.env.example`, `README.md` y `AGENTS.md` para documentar SMTP, secretos y operación.

**Fuera de alcance:**

- Controllers, DTOs, validadores y rutas HTTP; se definen en SPEC 04.
- Cambios directos a `specs/02-identidad-roles-jwt.md`.
- Registro anónimo o auto-registro.
- Recuperación ordinaria de contraseñas.
- Refresh tokens.
- Gmail API, OAuth 2.0 de Gmail y SDKs específicos de Gmail.
- Outbox transaccional, colas y procesamiento asíncrono.
- UI de activación alojada por la API.
- Nombre visible del usuario; el alta recibe únicamente email en esta fase.

## Modelo de datos y contratos

### Estado de cuenta

El dominio mantiene el estado funcional sin depender de Identity:

```text
UserStatus.PendingActivation
UserStatus.Active
```

Una cuenta `PendingActivation`:

- tiene exclusivamente el rol `User`;
- no tiene contraseña utilizable;
- no puede iniciar sesión;
- puede recibir una invitación nueva.

Una cuenta `Active` tiene contraseña establecida, email confirmado y puede autenticarse si las credenciales son correctas.

### Resultado de invitación

Application utiliza un resultado neutral equivalente a:

```text
UserInvitationResult
- User: User
- ActivationToken: string
```

El token solo circula entre Application e Infrastructure para construir el correo. Nunca forma parte de un response HTTP ni de un error público.

### Puertos

`IUserInvitationService` define:

- `RegisterPendingAsync(email)`;
- `ActivateAsync(email, token, newPassword)`;
- `ResendAsync(userId)`.

`IInvitationNotifier` representa la intención de notificar una invitación y no conoce SMTP, Gmail ni MailKit.

`IEmailSender` representa el transporte de correo:

```text
EmailMessage
- To
- Subject
- HtmlBody
- TextBody
- AttemptId
```

El identificador de intento solo sirve para correlación interna. SMTP no lo interpreta como una clave de idempotencia.

## Flujo funcional

### Registro

1. Application recibe el email ya validado por la capa HTTP.
2. Infrastructure comprueba que no exista una cuenta con ese email.
3. Se crea una cuenta `PendingActivation` con rol `User` y sin contraseña.
4. Se genera un token dedicado de Data Protection/Identity.
5. Se confirma la transacción de PostgreSQL.
6. `IInvitationNotifier` construye el enlace y renderiza la plantilla.
7. `IEmailSender` intenta entregar el mensaje.
8. Si SMTP falla después del commit, la cuenta permanece pendiente y se devuelve un error de entrega al consumidor autorizado.

### Reenvío

1. Solo una cuenta pendiente puede recibir un reenvío.
2. Se actualiza el security stamp para invalidar invitaciones anteriores.
3. Se genera un token nuevo.
4. Se confirma la transacción antes del envío.
5. Se entrega una invitación nueva con un `AttemptId` diferente.

### Activación

1. Se localiza la cuenta por `userId` o por el identificador definido en SPEC 04.
2. Se valida que permanezca pendiente.
3. Se valida el token dedicado y su vigencia.
4. Se establece la contraseña mediante la política de Identity.
5. Se marca la cuenta como activa y se confirma el email.
6. Se actualiza el security stamp dentro de la operación consistente.
7. El token deja de ser utilizable.

Tokens inválidos, vencidos, usados, pertenecientes a otra cuenta o asociados a una cuenta activa representan el mismo error de aplicación `invalid_activation_token`.

## Tokens y enlaces

- Usar un proveedor dedicado basado en Data Protection/Identity.
- Configurar `UserInvitation:TokenLifetimeHours`, con valor predeterminado de 24 horas.
- Rechazar valores no finitos o menores o iguales a cero durante el arranque.
- Usar la representación URL-safe soportada por el proveedor.
- No registrar tokens, enlaces completos, contraseñas ni security stamps.
- Construir el enlace con una URL HTTPS configurada.
- Colocar los datos de activación en el fragmento:

```text
https://cliente.local/activate#userId=<url-encoded>&token=<url-encoded>
```

El fragmento no se envía al servidor web del cliente, reduciendo la exposición del token en logs HTTP. La UI cliente debe extraerlo y enviarlo al endpoint definido en SPEC 04 mediante HTTPS.

## Correo SMTP reemplazable

### Separación de dependencias

La dirección de dependencias es:

```text
Application: IInvitationNotifier, IEmailSender
Infrastructure: InvitationEmailNotifier, MailKitSmtpEmailSender
Api: composición y configuración
```

MailKit solo puede aparecer en Infrastructure. Reemplazar MailKit por otro cliente SMTP requiere cambiar el adaptador y su registro, no Domain, Application ni los DTOs de Api.

### Configuración neutral

La configuración oficial usa la sección `Email:Smtp`:

- `Email:Smtp:Host`;
- `Email:Smtp:Port`;
- `Email:Smtp:SecurityMode`, con `StartTls` o `TlsOnConnect`;
- `Email:Smtp:Username`;
- `Email:Smtp:Password`;
- `Email:Smtp:FromAddress`;
- `Email:Smtp:FromName`;
- `Email:Smtp:TimeoutSeconds`.

En variables de entorno se representa con doble guion bajo:

```dotenv
Email__Smtp__Host='smtp.gmail.com'
Email__Smtp__Port='587'
Email__Smtp__SecurityMode='StartTls'
Email__Smtp__Username='sender@example.com'
Email__Smtp__Password='replace-with-a-provider-secret'
Email__Smtp__FromAddress='sender@example.com'
Email__Smtp__FromName='FEPP ContaOps'
Email__Smtp__TimeoutSeconds='30'
```

`.env.example` debe contener todas las variables necesarias con valores ficticios.
`.env` debe contener los valores locales reales, permanecer fuera de Git y no
incluirse en imágenes, respaldos sin cifrar ni documentación versionada.
ASP.NET Core no carga `.env` automáticamente; README debe explicar cómo
importar sus variables al proceso antes de ejecutar la API desde el host.

El adaptador debe rechazar SMTP sin cifrado, puertos incompatibles con el modo de seguridad, credenciales vacías, remitente inválido y timeout no positivo.

### Perfil Gmail inicial

La configuración local inicial utilizará:

- `smtp.gmail.com`;
- puerto `587` con `StartTls`, o puerto `465` con `TlsOnConnect`;
- la cuenta Gmail como `Username` y `FromAddress`;
- una contraseña de aplicación de Google como `Password`.

La contraseña de aplicación requiere verificación en dos pasos. Una API key de Google no autentica SMTP. Las credenciales deben vivir en User Secrets, variables de entorno o un gestor local de secretos; nunca en archivos versionados.

### Contrato de configuración local

La configuración de esta spec se expone mediante variables de entorno usando
doble guion bajo para representar secciones .NET:

| Variable                             | Obligatoria | Secreta | Regla                                                                             |
| ------------------------------------ | ----------: | ------: | --------------------------------------------------------------------------------- |
| `UserInvitation__TokenLifetimeHours` |          Sí |      No | Número positivo; valor inicial recomendado: `24`.                                 |
| `UserInvitation__ActivationBaseUrl`  |          Sí |      No | URL HTTPS del cliente de activación.                                              |
| `Email__Smtp__Host`                  |          Sí |      No | Host del proveedor SMTP.                                                          |
| `Email__Smtp__Port`                  |          Sí |      No | Puerto positivo compatible con el modo TLS.                                       |
| `Email__Smtp__SecurityMode`          |          Sí |      No | Solo `StartTls` o `TlsOnConnect`.                                                 |
| `Email__Smtp__Username`              |          Sí |      No | Cuenta autorizada para SMTP.                                                      |
| `Email__Smtp__Password`              |          Sí |      Sí | Contraseña SMTP o contraseña de aplicación.                                       |
| `Email__Smtp__FromAddress`           |          Sí |      No | Remitente válido y autorizado.                                                    |
| `Email__Smtp__FromName`              |          No |      No | Nombre visible del remitente.                                                     |
| `Email__Smtp__TimeoutSeconds`        |          Sí |      No | Número positivo.                                                                  |
| `Email__Templates__RootPath`         |          No |      No | Directorio alternativo de plantillas; por defecto se usa el catálogo distribuido. |
| `DataProtection__KeyRingPath`        | No en local |      No | Ruta persistente; obligatoria para Docker o despliegues persistentes.             |

Los valores secretos solo pueden configurarse mediante `.env` local no
versionado, User Secrets, variables del proceso o un mecanismo seguro del
entorno. `.env.example` debe mostrar placeholders y nunca valores reales.
La API debe fallar durante el arranque si falta una variable obligatoria o si
su valor incumple las reglas de validación.

Las siguientes variables son obsoletas y no forman parte del contrato de esta
API:

```text
MAILER_EMAIL
MAILER_SECRET_KEY
MAILER_FROM
MAIL_HOST
MAIL_PORT
MAIL_SECURE
MAIL_TEMPLATE_EXTENSION
```

La migración conceptual es:

```text
MAIL_HOST              -> Email__Smtp__Host
MAIL_PORT              -> Email__Smtp__Port
MAIL_SECURE            -> Email__Smtp__SecurityMode
MAILER_EMAIL           -> Email__Smtp__Username
MAILER_SECRET_KEY      -> Email__Smtp__Password
MAILER_FROM            -> Email__Smtp__FromAddress
```

`MAIL_TEMPLATE_EXTENSION` no tiene reemplazo directo: el catálogo se define
mediante `templates.json` y los archivos compañeros HTML/TXT.

### Entrega y errores

- Construir un `MimeMessage` multipart con HTML y texto plano.
- Respetar cancelación y timeout.
- Traducir errores de conexión, autenticación, protocolo y timeout a `email_delivery_unavailable`.
- No registrar destinatarios, cuerpos, tokens, enlaces ni credenciales.
- No reintentar automáticamente una operación cuyo resultado sea incierto.
- No afirmar idempotencia transaccional: Gmail SMTP puede aceptar el mensaje antes de perderse la respuesta.

## Plantillas

Las plantillas se versionan en Git dentro de Infrastructure:

```text
src/Fepp.ContaOps.Infrastructure/Email/Templates/
├── templates.json
├── invitation.html
└── invitation.txt
```

El directorio puede reemplazarse mediante `Email:Templates:RootPath` para desarrollo local o para un volumen montado, pero el catálogo base debe estar incluido en el repositorio y disponible en el output de la aplicación.

`templates.json` define por cada clave:

- archivo HTML;
- archivo de texto plano;
- asunto;
- parámetros requeridos.

El catálogo inicial debe contener la plantilla `invitation`. El diseño debe permitir agregar posteriormente `change-password`, `notification` u otras plantillas sin cambiar `IEmailSender`.

La plantilla recibe parámetros explícitos, como `userId` y `activationLink`. Cada parámetro debe codificarse según el contexto: HTML para `invitation.html` y texto plano para `invitation.txt`. La aplicación debe fallar al arrancar si falta una plantilla, un archivo o un parámetro declarado en el manifest.

## Data Protection y key ring

- `DataProtection:KeyRingPath` es opcional durante el desarrollo local.
- En ejecución local de Windows se puede proteger el key ring mediante DPAPI.
- En Docker se debe montar un named volume persistente para el directorio del key ring.
- El volumen debe conservar las claves al recrear el contenedor.
- El proceso debe tener permisos mínimos de lectura y escritura sobre el directorio.
- El disco anfitrión o volumen Docker debe contar con protección en reposo disponible en el equipo.
- ASP.NET Core administra la generación y rotación de claves.
- No se incorpora un servicio externo de gestión de claves en esta fase.

## Plan de implementación

1. Definir `UserStatus`, el resultado de invitación y los puertos neutrales en Domain/Application.
2. Implementar el adaptador Identity para cuentas pendientes, tokens dedicados y activación transaccional.
3. Implementar `UserInvitationOptions` y la validación de vigencia, URL y key ring.
4. Definir `Email:Smtp` y `Email:Templates` sin nombres específicos de Gmail en Application.
5. Crear `templates.json`, `invitation.html` e `invitation.txt` versionados.
6. Implementar el renderer parametrizable y `InvitationEmailNotifier` en Infrastructure.
7. Implementar `MailKitSmtpEmailSender` detrás de `IEmailSender`.
8. Registrar las implementaciones en el composition root sin filtrar credenciales.
9. Probar el transporte contra un servidor SMTP en proceso y probar errores de entrega.
10. Entregar a SPEC 04 los contratos necesarios para exponer registro, activación y reenvío por HTTP.
11. Actualizar `.env.example`, `README.md` y `AGENTS.md` con este contrato sin incluir secretos reales.

## Criterios de aceptación

- [x] Domain no referencia ASP.NET Core, Identity, EF Core, Gmail ni MailKit.
- [x] Application no referencia Api, Infrastructure, Gmail ni MailKit.
- [ ] Una cuenta registrada queda pendiente, sin contraseña utilizable y con rol exclusivamente `User`.
- [ ] Una cuenta pendiente no puede iniciar sesión.
- [ ] El registro confirma PostgreSQL antes de intentar SMTP.
- [ ] Un fallo SMTP conserva la cuenta pendiente.
- [ ] El reenvío invalida invitaciones anteriores.
- [ ] Una invitación usada o vencida no puede volver a activarse.
- [ ] La activación aplica la política de contraseña de Identity.
- [ ] El enlace usa HTTPS, `userId`, token URL-safe y fragmento de cliente.
- [ ] Ningún log, error o respuesta contiene token, contraseña, security stamp o enlace completo.
- [x] `IEmailSender` no expone tipos MailKit.
- [x] MailKit solo se utiliza en el adaptador de Infrastructure.
- [x] `Email:Smtp:SecurityMode` admite `StartTls` y `TlsOnConnect`, sin SMTP sin cifrado.
- [ ] Gmail funciona con contraseña de aplicación y no con API key.
- [x] El SMTP permite sustituir Gmail por otro proveedor compatible sin modificar Application.
- [ ] HTML y texto plano se construyen desde la misma clave de plantilla y parámetros.
- [x] El manifest valida archivos, asunto y parámetros al arrancar.
- [ ] Las plantillas pueden reemplazarse mediante `Email:Templates:RootPath`.
- [ ] El key ring local puede usar DPAPI y Docker utiliza un named volume persistente.
- [x] Las pruebas SMTP utilizan un servidor en proceso y nunca Gmail real.
- [x] `.env.example`, `README.md` y `AGENTS.md` documentan el mismo contrato de configuración.
- [x] Ningún archivo versionado contiene contraseñas, claves JWT o secretos SMTP reales.
- [x] Las variables `MAILER_*`, `MAIL_*` y `MAIL_TEMPLATE_EXTENSION` no se usan como configuración vigente.
- [ ] La configuración inválida o incompleta impide el arranque de la aplicación.

## Decisiones

- **Sí:** MailKit como adaptador inicial SMTP; es reemplazable por inversión de dependencias.
- **No:** acoplar Application a Gmail o MailKit.
- **Sí:** configuración neutral `Email:Smtp`.
- **No:** conservar `MAILER_*` como contrato oficial; esos nombres pertenecen al proyecto Node de referencia.
- **Sí:** usar `Email__Smtp__*` como representación de entorno de `Email:Smtp`.
- **No:** usar `MAILER_*` ni `MAIL_TEMPLATE_EXTENSION` como configuración oficial.
- **Sí:** contraseña de aplicación Gmail para el entorno inicial.
- **No:** API key de Google para autenticación SMTP.
- **Sí:** plantillas HTML y texto plano versionadas con manifest JSON.
- **Sí:** directorio configurable de plantillas para personalización operativa.
- **No:** introducir `displayName` hasta definir su persistencia y contrato.
- **Sí:** no reintentar automáticamente resultados SMTP inciertos.
- **No:** afirmar idempotencia de SMTP.
- **Sí:** key ring local opcional y named volume persistente para Docker.
- **No:** servicios externos de gestión de claves en esta fase.

## Riesgos

| Riesgo                                          | Mitigación                                                                |
| ----------------------------------------------- | ------------------------------------------------------------------------- |
| SMTP acepta un mensaje y se pierde la respuesta | No reintentar automáticamente; permitir reenvío administrativo explícito. |
| Se pierde el key ring                           | Persistir `DataProtection:KeyRingPath` y usar named volume en Docker.     |
| Una plantilla externa queda incompleta          | Validar manifest, archivos y parámetros durante el arranque.              |
| Un token aparece en logs del cliente            | Usar fragmento de URL y prohibir logging del enlace completo.             |
| Gmail cambia sus mecanismos de acceso           | Mantener `IEmailSender` y configuración SMTP neutral.                     |

## Cambios documentales obligatorios

- `.env.example` debe documentar PostgreSQL, pgAdmin, JWT, administrador,
  invitaciones, SMTP, plantillas y Data Protection mediante placeholders.
- `README.md` debe explicar Gmail como proveedor inicial, la contraseña de
  aplicación, la carga manual de `.env`, las plantillas y el key ring.
- `AGENTS.md` debe exigir que los secretos no se versionen, que las pruebas no
  conecten con Gmail real, que el contrato sea `Email:Smtp`/`Email__Smtp__*` y
  que MailKit permanezca aislado en Infrastructure.
- Los tres archivos deben permanecer sincronizados con esta spec.

## Qué no está en esta spec

- Endpoints HTTP, DTOs, validadores y políticas de autorización; pertenecen a SPEC 04.
- Corrección de los criterios pendientes de SPEC 02; SPEC 04 los verificará sin modificar SPEC 02.
- Outbox, colas, reintentos durables o entrega asíncrona.
- OAuth 2.0 de Gmail.
- Persistencia de nombre visible del usuario.
- Recuperación de contraseña.
