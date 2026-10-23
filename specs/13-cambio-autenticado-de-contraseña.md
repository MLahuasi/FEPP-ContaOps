# SPEC 13 — Cambio autenticado de contraseña con sesión web

> **Estado:** Borrador
> **Dependencias:** SPEC 09, SPEC 11, SPEC 12
> **Fecha:** 2026-10-20
> **Objetivo:** Permitir que un usuario autenticado cambie su contraseña y continúe con una sesión web nueva mientras se invalidan todas las credenciales anteriores.

## Alcance

**Incluye:**

- `POST /api/auth/change-password`.
- Request con `currentPassword` y `newPassword`.
- Validación mediante Identity sin duplicar reglas de complejidad.
- Cambio mediante `UserManager.ChangePasswordAsync`.
- Rotación de `SecurityStamp`.
- Revocación de sesión web anterior.
- Reemplazo de la cookie de sesión.
- Eventos de auditoría definidos en SPEC 12.
- Pruebas HTTP, seguridad y concurrencia.

**Fuera de alcance:**

- Recuperación de contraseña por correo.
- Restablecimiento sin sesión.
- Múltiples sesiones por usuario.
- Emisión de JWT o refresh tokens para el navegador.
- Cookies, CSRF y sesión web, cubiertos por SPEC 11.

## Contrato HTTP

```text
POST /api/auth/change-password
Cookie: __Host-fepp-session=<sesión>
X-CSRF-TOKEN: <token antiforgery>
```

```json
{
  "currentPassword": "<password>",
  "newPassword": "<password>"
}
```

Una operación exitosa devuelve `204 No Content`, reemplaza la cookie de sesión y no devuelve tokens. Una contraseña actual incorrecta o una nueva contraseña inválida devuelve `400 validation_error` sin revelar el motivo sensible.

## Plan de implementación

1. Ejecutar `docker compose ps`; no eliminar volúmenes ni reconfigurar pgAdmin.
2. Crear request y validador de límites de transporte sin duplicar la política Identity.
3. Crear el caso de uso de cambio de contraseña en Application.
4. Integrar `UserManager.ChangePasswordAsync` y traducir sus errores a `validation_error`.
5. Rotar el `SecurityStamp` solo después de un cambio exitoso.
6. Revocar la sesión web anterior y crear la nueva dentro de una operación consistente.
7. Reemplazar la cookie de sesión y devolver `204` sin secretos.
8. Exponer `POST /api/auth/change-password` con autenticación Cookie y protección CSRF.
9. Registrar eventos de éxito y rechazo según SPEC 12, sin contraseñas, tokens, hashes o stamps.
10. Ejecutar build, unitarias, integración, Swagger y pruebas de invalidación inmediata de la cookie anterior.

## Criterios de aceptación

- [ ] Solo un usuario autenticado puede cambiar su contraseña.
- [ ] La contraseña actual debe ser correcta.
- [ ] Identity aplica la política configurada.
- [ ] Un cambio exitoso rota el `SecurityStamp`.
- [ ] La cookie de sesión anterior queda inválida inmediatamente.
- [ ] La respuesta exitosa no contiene tokens ni secretos.
- [ ] Los errores devuelven `400 validation_error`.
- [ ] Los eventos de auditoría definidos en SPEC 12 se escriben con nivel correcto.
- [ ] Nunca se registran contraseñas ni tokens.

## Decisiones

- **Sí:** `POST /api/auth/change-password`.
- **Sí:** rotar y continuar con una sesión web nueva.
- **Sí:** revocación inmediata de la sesión anterior.
- **No:** emitir tokens si el cambio de contraseña falla.
- **No:** implementar recuperación de contraseña en esta spec.

## Qué no está en esta spec

- Recuperación o reset de contraseña sin autenticación.
- Sesiones simultáneas por usuario.
- Auditoría persistida en PostgreSQL.
