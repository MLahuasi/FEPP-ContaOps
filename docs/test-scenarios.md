# Escenarios de prueba de autenticación JWT

Este documento registra los escenarios validados para la autenticación JWT,
la sesión única, la revocación y la validación por solicitud.

> **Seguridad:** los valores sensibles utilizados durante las pruebas se
> representan como `<REDACTED>`. No deben versionarse contraseñas, JWT,
> claves de firma ni tokens de activación.

## Requisitos

- Ejecutar la API con `.\run-local.ps1`, que carga las variables de `.env`.
- Tener PostgreSQL disponible.
- Utilizar un usuario local de pruebas.
- Utilizar `/api/users/invitations` como endpoint protegido.

## 1. Claims y vigencia

### Procedimiento

Ejecutar `POST /api/auth/login`:

```json
{
  "email": "<TEST_USER_EMAIL>",
  "password": "<TEST_USER_PASSWORD>"
}
```

Resultado esperado: `200 OK`, con una respuesta de este formato:

```json
{
  "accessToken": "<JWT>",
  "tokenType": "Bearer",
  "expiresIn": 900
}
```

Decodificar el JWT sin modificarlo y verificar:

- `sid`: identificador de la sesión persistida.
- `jti`: identificador único del JWT.
- `sub`: identificador del usuario.
- `iss`: `FEPP-ContaOps`.
- `aud`: `FEPP-ContaOps.Api`.
- `exp`: fecha de expiración.
- `nbf`: fecha desde la que el token es válido.
- `sid` y `jti` deben ser valores únicos.

El token observado durante la prueba tuvo esta estructura:

```json
{
  "sub": "<USER_ID>",
  "sid": "<SESSION_ID>",
  "jti": "<JWT_ID>",
  "nbf": "<UNIX_TIME>",
  "exp": "<UNIX_TIME>",
  "iss": "FEPP-ContaOps",
  "aud": "FEPP-ContaOps.Api"
}
```

La respuesta incluye `expiresIn: 900`. En el payload observado se emite
`nbf`, no `iat`; por tanto, con esta implementación la diferencia verificable
es `exp - nbf = 900` segundos. Si el criterio exige literalmente `exp - iat`,
el JWT tendría que emitir también el claim `iat`.

## 2. Login sucesivo

### Procedimiento

1. Obtener `JWT-A` desde el cliente o equipo 1.
2. Obtener `JWT-B` desde el cliente o equipo 2 usando el mismo usuario.
3. Decodificar ambos tokens y confirmar que sus valores `sid` y `jti` son
   diferentes.
4. Enviar `JWT-A` a un endpoint protegido.

Encabezado utilizado:

```http
Authorization: Bearer <JWT-A>
```

Resultado esperado para `JWT-A`:

```http
401 Unauthorized
```

```json
{
  "type": "https://httpstatuses.com/401",
  "title": "Authentication failed.",
  "status": 401,
  "detail": "Authentication is required.",
  "instance": "/api/users/invitations",
  "code": "authentication_required",
  "traceId": "<TRACE_ID>"
}
```

5. Repetir la solicitud usando `JWT-B`.

`JWT-B` no debe responder `401`. Puede devolver una respuesta de negocio,
como `201 Created` o `409 resource_conflict` si el correo ya existe.

## 3. Persistencia de sesión

Consultar PostgreSQL o pgAdmin:

```sql
SELECT "SessionId", "UserId", "Status",
       "SecurityStamp", "CreatedAtUtc", "ExpiresAtUtc"
FROM "UserSessions"
ORDER BY "CreatedAtUtc" DESC;
```

Para el usuario probado se confirmó:

- Una sesión activa con `Status = 1`.
- La sesión anterior con `Status = 2`, revocada.
- Las sesiones históricas revocadas se conservan.

La sesión activa y la sesión anterior deben corresponder respectivamente a
`JWT-B` y `JWT-A` mediante su claim `sid`.

## 4. Cambio de `SecurityStamp`

### Procedimiento

1. Iniciar sesión y conservar el JWT.
2. Consultar el sello actual:

```sql
SELECT "Id", "Email", "SecurityStamp"
FROM "AspNetUsers"
WHERE "Id" = '<USER_ID>';
```

3. Cambiar el sello en la base de datos local:

```sql
UPDATE "AspNetUsers"
SET "SecurityStamp" = 'TEST-' || gen_random_uuid()::text
WHERE "Id" = '<USER_ID>';
```

4. Comprobar el nuevo valor:

```sql
SELECT "SecurityStamp"
FROM "AspNetUsers"
WHERE "Id" = '<USER_ID>';
```

5. Usar el JWT anterior en `/api/users/invitations`.

Resultado observado: `401` con `code: authentication_required`.

La respuesta no revela que el motivo fue el cambio del `SecurityStamp`.

## 5. Sesión expirada

### Procedimiento

1. Iniciar sesión y conservar el JWT.
2. Obtener el `sid` del JWT.
3. Modificar `ExpiresAtUtc` de la sesión para establecer una fecha pasada:

```sql
UPDATE "UserSessions"
SET "ExpiresAtUtc" = NOW() - INTERVAL '1 minute'
WHERE "SessionId" = '<SESSION_ID>';
```

4. Usar el JWT sin modificarlo en el endpoint protegido.

Resultado esperado: `401` con `code: authentication_required`.

## 6. Sesión inexistente o revocada

### Localizar la sesión con el `sid`

```sql
SELECT "SessionId", "UserId", "Status", "ExpiresAtUtc"
FROM "UserSessions"
WHERE "SessionId" = '<SESSION_ID>';
```

### Revocar la sesión

Se utilizó la revocación para conservar el historial:

```sql
UPDATE "UserSessions"
SET
    "Status" = 2,
    "RevokedAtUtc" = NOW(),
    "RevocationReason" = 'ManualSecurityTest'
WHERE "SessionId" = '<SESSION_ID>'
  AND "Status" = 1;
```

`Status = 2` corresponde a una sesión revocada.

### Comprobar el resultado

```sql
SELECT "SessionId", "Status", "RevokedAtUtc", "RevocationReason"
FROM "UserSessions"
WHERE "SessionId" = '<SESSION_ID>';
```

Repetir inmediatamente la solicitud protegida con el mismo JWT.

Resultado observado:

```http
401 Unauthorized
```

```json
{
  "type": "https://httpstatuses.com/401",
  "title": "Authentication failed.",
  "status": 401,
  "detail": "Authentication is required.",
  "instance": "/api/users/invitations",
  "code": "authentication_required",
  "traceId": "<TRACE_ID>"
}
```

La respuesta no revela si el rechazo fue causado por expiración, revocación o
cambio del sello de seguridad.

## 7. Firma, issuer y audience

### Importante sobre el enlace de activación

El enlace siguiente corresponde a una activación:

```text
https://localhost:7077/activate#userId=<USER_ID>&token=<ACTIVATION_TOKEN>
```

El token de activación es opaco y no es un JWT. Por ello, no permite validar
firma, issuer o audience.

### Firma incorrecta en el escenario ejecutado

Se modificó un carácter del token de activación y se envió a
`POST /api/auth/activate`:

```json
{
  "userId": "<USER_ID>",
  "token": "<MODIFIED_ACTIVATION_TOKEN>",
  "newPassword": "<NEW_PASSWORD>"
}
```

Resultado observado:

```http
400 Bad Request
```

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Request validation failed.",
  "status": 400,
  "detail": "The activation token is invalid or expired.",
  "instance": "/api/auth/activate",
  "code": "invalid_activation_token",
  "traceId": "<TRACE_ID>"
}
```

Esto valida correctamente que el token de activación modificado es rechazado,
pero no es una prueba de firma JWT.

### Issuer y audience incorrectos

1. Ejecutar login y conservar el JWT emitido por la API.
2. Detener la API.
3. Cambiar temporalmente en `.env` únicamente una propiedad:

```dotenv
Jwt__Issuer=<ISSUER_INCORRECTO>
```

o:

```dotenv
Jwt__Audience=<AUDIENCE_INCORRECTA>
```

4. Iniciar nuevamente la API con `.\run-local.ps1`.
5. Enviar el JWT original, sin modificarlo, a `/api/users/invitations`.

Resultado observado para issuer y audience incorrectos: `401` con
`code: authentication_required`.

6. Restaurar los valores originales en `.env` y reiniciar la API:

```dotenv
Jwt__Issuer=FEPP-ContaOps
Jwt__Audience=FEPP-ContaOps.Api
```

No se debe modificar el payload del JWT para estas pruebas, porque hacerlo
invalidaría también la firma.

## 8. Consulta por request

### Procedimiento

1. Ejecutar login y conservar un JWT válido.
2. Realizar una solicitud protegida a `/api/users/invitations`.

Resultado observado antes de revocar la sesión:

```http
409 Conflict
```

```json
{
  "type": "https://httpstatuses.com/409",
  "title": "Resource conflict.",
  "status": 409,
  "detail": "A user with this email already exists.",
  "instance": "/api/users/invitations",
  "code": "resource_conflict",
  "traceId": "<TRACE_ID>"
}
```

3. Revocar directamente en PostgreSQL la sesión asociada al `sid`.
4. Repetir inmediatamente la solicitud con el mismo JWT.

Resultado observado después de revocar:

```http
401 Unauthorized
```

```json
{
  "type": "https://httpstatuses.com/401",
  "title": "Authentication failed.",
  "status": 401,
  "detail": "Authentication is required.",
  "instance": "/api/users/invitations",
  "code": "authentication_required",
  "traceId": "<TRACE_ID>"
}
```

El resultado demuestra que la sesión se consulta en cada solicitud y que no
existe caching positivo de una autenticación previamente válida.
