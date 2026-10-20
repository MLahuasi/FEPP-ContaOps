# SPEC 12 — Cambio autenticado de contraseña

> **Estado:** Borrador
> **Dependencias:** SPEC 09, SPEC 10, SPEC 11
> **Fecha:** 2026-10-20
> **Objetivo:** Permitir que un usuario autenticado cambie su contraseña y continúe con una sesión nueva mientras se invalidan todas las credenciales anteriores.

## Alcance

**Incluye:**

- `POST /api/auth/change-password`.
- Request con `currentPassword` y `newPassword`.
- Validación mediante Identity sin duplicar reglas de complejidad.
- Cambio mediante `UserManager.ChangePasswordAsync`.
- Rotación de `SecurityStamp`.
- Revocación de sesión y tokens anteriores.
- Emisión de JWT y refresh token nuevos.
- Eventos de auditoría exitosos y rechazados.
- Pruebas HTTP, seguridad y concurrencia.

**Fuera de alcance:**

- Recuperación de contraseña por correo.
- Restablecimiento sin sesión.
- Múltiples sesiones por usuario.
- Cookies de sesión.

## Contrato HTTP

```text
POST /api/auth/change-password
Authorization: Bearer <access-token>
```

```json
{
  "currentPassword": "<password>",
  "newPassword": "<password>"
}
```

Una operación exitosa devuelve `200 OK` con los nuevos tokens. Una contraseña actual incorrecta o una nueva contraseña inválida devuelve `400 validation_error` sin revelar el motivo sensible.

## Plan de implementación

1. Ejecutar `docker compose ps`; no eliminar volúmenes ni reconfigurar pgAdmin.
2. Crear request, response y validador de límites de transporte sin duplicar la política Identity.
3. Crear el caso de uso de cambio de contraseña en Application.
4. Integrar `UserManager.ChangePasswordAsync` y traducir sus errores a `validation_error`.
5. Rotar el `SecurityStamp` solo después de un cambio exitoso.
6. Revocar la sesión y refresh token anteriores dentro de una operación consistente.
7. Crear la sesión nueva y emitir JWT y refresh token nuevos.
8. Exponer `POST /api/auth/change-password` sin que el controller acceda a implementaciones técnicas.
9. Registrar eventos de éxito y rechazo sin contraseñas, tokens, hashes o stamps.
10. Ejecutar build, unitarias, integración, Swagger y pruebas de que los tokens anteriores reciben 401.

## Criterios de aceptación

- [ ] Solo un usuario autenticado puede cambiar su contraseña.
- [ ] La contraseña actual debe ser correcta.
- [ ] Identity aplica la política configurada.
- [ ] Un cambio exitoso rota el SecurityStamp.
- [ ] JWT y refresh token anteriores quedan inválidos inmediatamente.
- [ ] La respuesta exitosa contiene tokens nuevos y no contiene secretos antiguos.
- [ ] Los errores devuelven `400 validation_error`.
- [ ] Los eventos de auditoría se escriben con nivel correcto.
- [ ] Nunca se registran contraseñas ni tokens.

## Decisiones

- **Sí:** `POST /api/auth/change-password`.
- **Sí:** rotar y continuar con una sesión nueva.
- **Sí:** revocación inmediata de la sesión anterior.
- **No:** emitir un JWT nuevo si el cambio de contraseña falla.
- **No:** implementar recuperación de contraseña en esta spec.

## Qué no está en esta spec

- Recuperación o reset de contraseña sin autenticación.
- Sesiones simultáneas por usuario.
- Auditoría persistida en PostgreSQL.
