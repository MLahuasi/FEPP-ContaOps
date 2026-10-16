# SPEC 04 — API de invitaciones y validación de identidad

> **Estado:** Borrador
> **Dependencias:** SPEC 01, SPEC 02, SPEC 03
> **Fecha:** 2026-10-16
> **Objetivo:** Exponer el ciclo de invitaciones por HTTP y cerrar mediante pruebas los contratos pendientes de identidad, autorización, configuración y respuestas uniformes.

## Por qué existe esta spec

SPEC 03 define el núcleo de Application e Infrastructure, pero no decide el contrato HTTP. SPEC 02 conserva pendientes de validación que deben comprobarse cuando existan operaciones administrativas reales. Esta spec reúne esos endpoints y sus pruebas sin modificar el documento de SPEC 02.

## Alcance

**Incluye:**

- Controllers, DTOs y mapeo HTTP para invitaciones.
- Validación de requests en Api.
- Autorización `AdminOnly` para crear y reenviar invitaciones.
- Activación anónima mediante token recibido por el cliente.
- Responses que no exponen entidades técnicas, tokens ni contraseñas.
- ProblemDetails uniforme con `code` y `traceId`.
- Configuración validada de Identity, JWT, administrador inicial y usuario pendiente.
- Corrección y verificación de los criterios pendientes de SPEC 02.
- Pruebas unitarias e integración con PostgreSQL efímero.
- Actualización coordinada de `.env.example`, `README.md` y `AGENTS.md` para reflejar el flujo de invitación y la configuración segura de identidad.

**Fuera de alcance:**

- Implementación de MailKit, SMTP o plantillas; pertenece a SPEC 03.
- Modificación de `specs/02-identidad-roles-jwt.md`.
- Registro público o auto-registro.
- Recuperación de contraseña.
- Refresh tokens.
- Permisos funcionales sobre el balance.
- Creación de un `BootstrapUser` separado del flujo de invitación.

## Contratos HTTP

### Crear invitación

```text
POST /api/users/invitations
Authorization: Bearer <admin-jwt>
Content-Type: application/json
```

Request:

```json
{
  "email": "user@example.com"
}
```

Response exitosa:

```http
201 Created
```

```json
{
  "id": "<user-id>",
  "email": "user@example.com",
  "role": "User"
}
```

La respuesta no incluye token, enlace, contraseña, security stamp ni datos de SMTP.

Si PostgreSQL confirma la cuenta pero el correo no puede entregarse, la respuesta administrativa es:

```http
503 Service Unavailable
```

El `ProblemDetails` puede incluir el `userId` en una extensión administrativa segura para permitir el reenvío. Nunca incluye token ni enlace completo.

### Activar cuenta

```text
POST /api/auth/activate
```

Request:

```json
{
  "userId": "<user-id>",
  "token": "<opaque-token>",
  "newPassword": "<password>"
}
```

Response exitosa:

```http
204 No Content
```

La operación es anónima, pero solo puede completarse con un token válido asociado al usuario indicado.

### Reenviar invitación

```text
POST /api/users/{id}/invitations
Authorization: Bearer <admin-jwt>
```

Response exitosa:

```http
204 No Content
```

Solo funciona para cuentas pendientes. No acepta email en el body y no permite reenviar invitaciones a cuentas activas.

## Validación y mapeo HTTP

La capa Api valida forma, límites y formato:

- email obligatorio y con formato válido cuando corresponda;
- `userId` obligatorio y con formato permitido por el adaptador;
- token obligatorio y con longitud máxima definida;
- contraseña no vacía y dentro de los límites de transporte.

La complejidad de la contraseña no se duplica en FluentValidation. Identity aplica la política configurada durante la activación.

Application devuelve errores propios. Api los convierte así:

| Situación | HTTP | Código |
|---|---:|---|
| Request inválido | 400 | `validation_error` |
| Token inválido, vencido, usado o cuenta activa | 400 | `invalid_activation_token` |
| Email ya registrado | 409 | `resource_conflict` |
| Usuario inexistente para reenvío | 404 | `resource_not_found` |
| Usuario activo para reenvío | 409 | `resource_conflict` |
| SMTP no disponible | 503 | `email_delivery_unavailable` |
| Sin JWT en endpoint administrativo | 401 | `authentication_required` |
| JWT válido sin rol Admin | 403 | `forbidden` |
| Excepción inesperada | 500 | `unexpected_error` |

Todos los errores deben usar `application/problem+json`, contener `code` y `traceId`, y evitar enumerar usuarios, tokens o credenciales.

## Autorización

- `POST /api/users/invitations` requiere la política `AdminOnly`.
- `POST /api/users/{id}/invitations` requiere la política `AdminOnly`.
- `POST /api/auth/activate` permite acceso anónimo.
- Un JWT con rol `User` recibe 403 en los endpoints administrativos.
- Una solicitud sin JWT recibe 401 con `authentication_required` y `traceId`.
- La respuesta de autorización no revela detalles de la cuenta objetivo.

## Configuración de identidad

La implementación debe validar al arrancar:

### JWT

- `Jwt:Issuer` obligatorio.
- `Jwt:Audience` obligatorio.
- `Jwt:SigningKey` secreto de al menos 32 bytes.
- `Jwt:AccessTokenLifetimeMinutes` mayor que cero.

En variables de entorno se expresan como `Jwt__Issuer`, `Jwt__Audience`,
`Jwt__SigningKey` y `Jwt__AccessTokenLifetimeMinutes`.

### Administrador inicial

- `BootstrapAdmin:Email` obligatorio y válido.
- `BootstrapAdmin:Password` obligatorio y compatible con la política de Identity.
- El seed crea el administrador si no existe.
- El seed no reemplaza la contraseña de un administrador existente.

### Política de contraseña

Crear configuración validada para:

- longitud mínima, por defecto 12;
- mayúscula obligatoria;
- minúscula obligatoria;
- dígito obligatorio;
- símbolo obligatorio;
- seis caracteres únicos por defecto.

Estas opciones deben poder configurarse mediante `Identity__Password__*`, por
ejemplo `RequiredLength`, `RequireUppercase`, `RequireLowercase`, `RequireDigit`,
`RequireNonAlphanumeric` y `RequiredUniqueChars`.

### Bloqueo

Crear configuración validada para:

- bloqueo habilitado para cuentas nuevas;
- máximo de cinco intentos fallidos por defecto;
- duración de bloqueo de 15 minutos por defecto.

Estas opciones deben poder configurarse mediante `Identity__Lockout__*`, por
ejemplo `AllowedForNewUsers`, `MaxFailedAccessAttempts` y
`DefaultLockoutTimeSpanMinutes`.

La configuración puede mantener los defaults actuales, pero debe poder cambiarse sin editar código y debe fallar al iniciar si contiene valores inválidos.

### Usuarios regulares

No se conserva un `BootstrapUser` operativo independiente. Toda cuenta regular debe originarse en el flujo administrativo de invitación de SPEC 03. Las pruebas deben crear usuarios mediante ese flujo o usar dobles controlados.

Por tanto, `BootstrapUser__Email` y `BootstrapUser__Password` no forman parte
del contrato vigente de `.env.example`, `README.md` ni `AGENTS.md`.

## Contrato de configuración de entorno

El futuro `.env.example` debe conservar las variables de infraestructura y
configuración base siguientes, utilizando placeholders y no credenciales reales:

```text
POSTGRES_PASSWORD
PGADMIN_DEFAULT_EMAIL
PGADMIN_DEFAULT_PASSWORD
ConnectionStrings__Default
Jwt__Issuer
Jwt__Audience
Jwt__SigningKey
Jwt__AccessTokenLifetimeMinutes
BootstrapAdmin__Email
BootstrapAdmin__Password
```

Los correos de ejemplo deben ser ficticios, como `admin@example.local`.

La configuración de contraseña se expresará mediante:

```text
Identity__Password__RequiredLength
Identity__Password__RequireUppercase
Identity__Password__RequireLowercase
Identity__Password__RequireDigit
Identity__Password__RequireNonAlphanumeric
Identity__Password__RequiredUniqueChars
```

La configuración de bloqueo se expresará mediante:

```text
Identity__Lockout__AllowedForNewUsers
Identity__Lockout__MaxFailedAccessAttempts
Identity__Lockout__DefaultLockoutTimeSpanMinutes
```

Los valores iniciales definidos por esta spec son:

| Opción | Valor inicial |
|---|---:|
| `RequiredLength` | `12` |
| `RequireUppercase` | `true` |
| `RequireLowercase` | `true` |
| `RequireDigit` | `true` |
| `RequireNonAlphanumeric` | `true` |
| `RequiredUniqueChars` | `6` |
| `AllowedForNewUsers` | `true` |
| `MaxFailedAccessAttempts` | `5` |
| `DefaultLockoutTimeSpanMinutes` | `15` |

La configuración de invitaciones, SMTP, plantillas y Data Protection se define
en SPEC 03 y debe incluirse también en el `.env.example` consolidado. No se
deben duplicar nombres ni introducir una segunda convención de variables.

Las siguientes variables quedan eliminadas del contrato:

```text
BootstrapUser__Email
BootstrapUser__Password
```

No deben aparecer como variables operativas en `.env.example`, `README.md` ni
`AGENTS.md`. Los usuarios regulares se crean exclusivamente mediante el flujo
de invitación administrativa.

## Respuestas y seguridad

- No devolver entidades de Identity, EF Core o Domain directamente.
- Mapear manualmente a DTOs de response.
- No devolver tokens de invitación después de crear o reenviar.
- No registrar cuerpos HTTP sensibles.
- No incluir contraseñas, API keys, secretos SMTP, tokens, enlaces completos o security stamps en logs.
- Propagar cancelación del request sin convertirla en error interno.
- Usar el handler global para excepciones no previstas.

## Pruebas requeridas

### Unitarias

- Validadores de los tres requests.
- Mapeo de cada `ApplicationError` a su status y código HTTP.
- Política `AdminOnly` con rol Admin y User.
- Configuración inválida de JWT, administrador, contraseña y bloqueo.
- Seed idempotente y no sobrescritura de contraseña.
- Login uniforme para usuario inexistente, pendiente, sin contraseña, bloqueado y contraseña incorrecta.

### Integración HTTP

Usar PostgreSQL efímero y un sender de correo falso o el servidor SMTP en proceso de SPEC 03. No conectar Gmail real.

Cubrir como mínimo:

1. Admin crea una invitación y recibe 201.
2. El usuario creado tiene únicamente rol User.
3. El usuario pendiente no puede iniciar sesión.
4. La solicitud sin JWT recibe 401 con `code` y `traceId`.
5. Un User recibe 403 con `code` y `traceId`.
6. Un email duplicado recibe 409.
7. El cliente activa la cuenta con token válido y recibe 204.
8. El usuario activado puede iniciar sesión.
9. El mismo token usado de nuevo recibe 400 `invalid_activation_token`.
10. Un token inválido, vencido o de otro usuario recibe el mismo 400.
11. El administrador reenvía una invitación pendiente.
12. El token anterior deja de funcionar después del reenvío.
13. El reenvío de una cuenta activa recibe 409.
14. El reenvío de un usuario inexistente recibe 404.
15. Un fallo del sender produce 503 y conserva la cuenta pendiente.
16. Las respuestas nunca contienen token, contraseña, security stamp ni enlace completo.

## Plan de implementación

1. Definir los DTOs y responses HTTP sin exponer entidades internas.
2. Implementar validadores y el filtro de validación uniforme.
3. Implementar los controllers y mapearlos a los casos de uso de Application.
4. Implementar la política `AdminOnly` en las rutas de creación y reenvío.
5. Completar el mapeo de errores 400, 401, 403, 404, 409, 503 y 500.
6. Introducir las opciones configurables de contraseña y bloqueo de Identity.
7. Endurecer el seed para que sea idempotente y no sobrescriba contraseñas.
8. Verificar login uniforme de cuentas pendientes, bloqueadas y sin contraseña.
9. Agregar pruebas unitarias de configuración, autorización, validación y mapeo HTTP.
10. Agregar pruebas de integración con PostgreSQL efímero y correo falso/SMTP en proceso.
11. Actualizar Swagger y documentación operativa sin modificar SPEC 02.
12. Actualizar `.env.example` con JWT, `BootstrapAdmin__*`, `Identity__Password__*` e `Identity__Lockout__*`, usando placeholders.
13. Actualizar `README.md` con el flujo de invitación, endpoints, activación y eliminación de `BootstrapUser`.
14. Actualizar `AGENTS.md` para sincronizar configuración, arquitectura, secretos, pruebas y estado del repositorio.
15. Consolidar en `.env.example` las variables de SPEC 03 y SPEC 04, eliminando `BootstrapUser__*` y cualquier secreto real.

## Criterios de aceptación

- [ ] Los endpoints usan exactamente las rutas y métodos definidos en esta spec.
- [ ] Solo Admin puede crear o reenviar invitaciones.
- [ ] User recibe 403 y una solicitud sin JWT recibe 401 en endpoints administrativos.
- [ ] 401 y 403 contienen `code` y `traceId`.
- [ ] El response de creación nunca contiene token ni enlace.
- [ ] Activar una cuenta válida devuelve 204 y permite login posterior.
- [ ] Una cuenta pendiente no puede autenticarse, incluso con contraseña proporcionada.
- [ ] Tokens usados, inválidos, vencidos o de otra cuenta producen el mismo 400.
- [ ] Un reenvío invalida el token anterior.
- [ ] Email duplicado produce 409.
- [ ] Usuario inexistente para reenvío produce 404.
- [ ] Reenvío de usuario activo produce 409.
- [ ] Fallo de correo produce 503 y conserva la cuenta pendiente.
- [ ] La respuesta 503 administrativa puede incluir `userId`, pero nunca token ni enlace.
- [ ] El seed no sobrescribe la contraseña de un administrador existente.
- [ ] La aplicación falla al arrancar con configuración obligatoria inválida.
- [ ] La política de contraseña y bloqueo se configura sin editar código.
- [ ] Las pruebas cubren todos los escenarios mínimos definidos.
- [ ] SPEC 02 permanece sin modificaciones.
- [ ] `.env.example`, `README.md` y `AGENTS.md` no documentan `BootstrapUser` como flujo operativo.
- [ ] Los tres archivos documentan consistentemente JWT, administrador y políticas de Identity.
- [ ] Ningún archivo versionado contiene credenciales o secretos reales.
- [ ] `.env.example` conserva PostgreSQL, pgAdmin, conexión, JWT y `BootstrapAdmin__*`.
- [ ] `.env.example` incluye `Identity__Password__*`, `Identity__Lockout__*` y las variables de SPEC 03.
- [ ] `BootstrapUser__Email` y `BootstrapUser__Password` no forman parte del contrato vigente.
- [ ] Las variables de entorno usan una única convención con doble guion bajo.
- [ ] La configuración obligatoria inválida impide el arranque.

## Decisiones

- **Sí:** rutas centradas en usuarios e invitaciones.
- **No:** mantener `/api/auth/register` como ruta principal del registro administrativo.
- **Sí:** `userId` y token en el fragmento del enlace de cliente.
- **No:** incluir el email en la URL de activación.
- **Sí:** el error 503 administrativo puede incluir el `userId` para permitir reenvío.
- **No:** incluir tokens o enlaces completos en ProblemDetails.
- **Sí:** cerrar aquí los pendientes verificables de SPEC 02.
- **No:** modificar el documento de SPEC 02.
- **Sí:** crear cuentas regulares únicamente por invitación.
- **No:** conservar `BootstrapUser` como flujo operativo separado.
- **Sí:** mantener `.env.example`, `README.md` y `AGENTS.md` sincronizados con esta spec.
- **No:** documentar `BootstrapUser__*` como variables vigentes.
- **Sí:** usar PostgreSQL efímero y correo falso/SMTP en proceso.
- **No:** conectar pruebas a Gmail real.

## Riesgos

| Riesgo | Mitigación |
|---|---|
| La configuración de autenticación diverge entre emisión y validación | Validar JWT al arrancar y probar issuer, audience, firma y expiración mediante HTTP. |
| El seed cambia una contraseña existente | Test de idempotencia que ejecute el seed más de una vez y compruebe la contraseña original. |
| Un usuario pendiente se autentica por una ruta alternativa | Comprobar el estado antes de verificar credenciales y cubrirlo por integración. |
| Se filtra información en errores de autorización | Respuestas uniformes sin detalles de existencia o estado de usuarios. |
| La API arranca con una política de contraseña incoherente | Validar límites y relaciones de opciones durante el arranque. |

## Cambios documentales obligatorios

- `.env.example` debe incluir placeholders para `Jwt__*`, `BootstrapAdmin__*`,
  `Identity__Password__*` e `Identity__Lockout__*`, además de referenciar la
  configuración de invitaciones y SMTP de SPEC 03.
- `README.md` debe documentar que los usuarios regulares se crean únicamente
  mediante invitación, los endpoints de esta spec, la carga manual de `.env` y
  el fallo de arranque ante configuración inválida.
- `AGENTS.md` debe eliminar instrucciones obsoletas sobre `BootstrapUser`,
  exigir secretos fuera de archivos versionados y mantener README y
  `.env.example` sincronizados.
- Estos cambios documentales no modifican `specs/02-identidad-roles-jwt.md`.

## Qué no está en esta spec

- Implementación del transporte MailKit y del catálogo de plantillas; pertenece a SPEC 03.
- Cambios al texto de SPEC 02.
- Refresh tokens, recuperación de contraseña y permisos del balance.
- Usuario regular sembrado independientemente del flujo de invitación.
