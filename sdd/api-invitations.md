# API de invitaciones y activación

Esta guía describe el flujo HTTP de invitaciones implementado por FEPP-ContaOps.
La documentación OpenAPI está disponible en `/swagger` cuando Swagger está
habilitado y en `/swagger/v1/swagger.json`.

## Autenticación

Los endpoints administrativos requieren un JWT Bearer con el rol `Admin`.

```http
Authorization: Bearer <access-token>
```

La cuenta regular se crea mediante invitación. La activación es anónima y no
requiere enviar un JWT.

## Endpoints

### Crear invitación

```http
POST /api/users/invitations
Content-Type: application/json
Authorization: Bearer <admin-jwt>
```

```json
{
  "email": "user@example.com"
}
```

Respuesta exitosa: `201 Created`.

```json
{
  "id": "<user-id>",
  "email": "user@example.com",
  "role": "User"
}
```

La respuesta no contiene token, enlace de activación, contraseña, security
stamp ni datos SMTP. El correo de invitación se entrega después de confirmar la
persistencia de la cuenta.

### Activar cuenta

```http
POST /api/auth/activate
Content-Type: application/json
```

```json
{
  "userId": "<user-id>",
  "token": "<opaque-token>",
  "newPassword": "<password>"
}
```

Respuesta exitosa: `204 No Content`.

El token es de un solo uso. La política de complejidad de la contraseña la
aplica Identity; la API solo valida los límites de transporte.

### Reenviar invitación

```http
POST /api/users/{id}/invitations
Authorization: Bearer <admin-jwt>
```

Respuesta exitosa: `204 No Content`.

Solo se puede reenviar una invitación de una cuenta pendiente. El reenvío
invalida el token anterior.

## Errores

Todos los errores se devuelven como `application/problem+json` e incluyen:

- `code`: código estable del error.
- `traceId`: identificador de correlación.

| Situación | HTTP | `code` |
|---|---:|---|
| Request inválido | 400 | `validation_error` |
| Token inválido, vencido o usado | 400 | `invalid_activation_token` |
| Sin JWT | 401 | `authentication_required` |
| JWT sin rol `Admin` | 403 | `forbidden` |
| Usuario inexistente para reenvío | 404 | `resource_not_found` |
| Email duplicado o cuenta activa para reenvío | 409 | `resource_conflict` |
| Fallo de entrega de correo | 503 | `email_delivery_unavailable` |

Las respuestas de error no enumeran cuentas ni exponen tokens, contraseñas,
enlaces completos, security stamps o credenciales de correo.
