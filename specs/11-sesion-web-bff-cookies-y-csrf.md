# SPEC 11 — Sesión web BFF, cookies seguras y CSRF

> **Estado:** Borrador
> **Dependencias:** SPEC 09, SPEC 10
> **Fecha:** 2026-10-22
> **Objetivo:** Convertir la API actual en el BFF del navegador para evitar que access tokens y refresh tokens sean entregados o almacenados por JavaScript.

## Alcance

**Incluye:**

- Sesión web persistente administrada por el servidor.
- Cookie `__Host-` `HttpOnly`, `Secure` y `SameSite`.
- Data Protection exclusivamente para cookies y antiforgery.
- Persistencia segura y compartida del key ring.
- Login sin devolver JWT ni refresh token al navegador.
- `GET /api/auth/session` con datos públicos mínimos.
- Logout con revocación persistente y eliminación de cookie.
- Protección CSRF para operaciones mutables.
- Expiración absoluta de siete días e inactividad.
- Pruebas de seguridad, concurrencia y redacción de secretos.

**Fuera de alcance:**

- Clientes móviles o integraciones externas.
- Múltiples sesiones simultáneas por usuario.
- Recuperación o reset de contraseña.
- Auditoría detallada, cubierta por SPEC 12.
- Cambio de contraseña, cubierto por SPEC 13.
- Uso de JWT y refresh tokens directamente desde el navegador.

## Contratos

`POST /api/auth/login` recibe credenciales y devuelve `204 No Content` con una cookie `__Host-fepp-session`. No devuelve access token, refresh token ni identificadores internos.

`GET /api/auth/session` requiere la cookie y devuelve únicamente datos públicos mínimos:

```json
{
  "authenticated": true,
  "email": "usuario@example.com",
  "roles": ["User"]
}
```

`POST /api/auth/logout` requiere cookie y token antiforgery, revoca la sesión, elimina la cookie y devuelve `204 No Content`.

La cookie debe tener `HttpOnly=true`, `Secure=true`, `SameSite=Strict`, `Path=/`, nombre `__Host-fepp-session` y ningún atributo `Domain`.

## Data Protection

ASP.NET Core Data Protection se reintroduce exclusivamente para proteger cookies de autenticación y tokens antiforgery. Esta decisión no modifica históricamente SPEC 07, que eliminó consumidores operativos anteriores.

El key ring debe persistirse fuera del árbol fuente, protegerse mediante configuración segura, compartirse entre instancias, validarse durante el arranque y no contener secretos en archivos versionados.

## Plan de implementación

1. Ejecutar `docker compose ps`, revisar el flujo JWT actual y definir la migración de sesiones existentes sin eliminar volúmenes.
2. Reintroducir Data Protection exclusivamente para Cookie Authentication y Antiforgery, con key ring seguro y configuración validada.
3. Implementar la persistencia de sesiones web, identificadores opacos, expiración absoluta de siete días e inactividad.
4. Configurar Cookie Authentication con `__Host-`, `HttpOnly`, `Secure`, `SameSite`, validación persistente y errores JSON.
5. Configurar Antiforgery y exigir CSRF en operaciones mutables autenticadas.
6. Cambiar login para crear la sesión, emitir la cookie y no devolver access ni refresh tokens.
7. Implementar `GET /api/auth/session` con datos públicos mínimos y sin secretos.
8. Cambiar logout para revocar la sesión, eliminar la cookie y devolver `204`.
9. Retirar el flujo JWT/refresh del contrato público web, actualizar Swagger y definir la transición de datos históricos.
10. Ejecutar build, pruebas unitarias, integración y validaciones manuales de cookies, CSRF, expiración, revocación y logs.

## Criterios de aceptación

- [ ] Login no devuelve access ni refresh tokens.
- [ ] La cookie es `__Host-`, `HttpOnly`, `Secure` y `SameSite` seguro.
- [ ] Ningún secreto de autenticación está disponible mediante JavaScript.
- [ ] Los endpoints protegidos validan la sesión persistente.
- [ ] Logout invalida inmediatamente la cookie y la sesión.
- [ ] Una sesión revocada devuelve `401 Unauthorized`.
- [ ] El cambio de `SecurityStamp` invalida la sesión.
- [ ] La sesión no supera siete días.
- [ ] Se aplica expiración por inactividad.
- [ ] Las operaciones mutables sin CSRF son rechazadas.
- [ ] Login y respuestas de sesión usan `Cache-Control: no-store`.
- [ ] No se registran cookies, contraseñas ni tokens antiforgery.
- [ ] Swagger representa correctamente endpoints anónimos y protegidos.
- [ ] Las pruebas cubren CSRF, logout, expiración, revocación, concurrencia y múltiples instancias.

## Decisiones

- **Sí:** convertir `Fepp.ContaOps.Api` en BFF del navegador.
- **Sí:** mantener la vigencia absoluta máxima de siete días.
- **Sí:** usar sesiones opacas persistidas en servidor.
- **Sí:** compartir el key ring entre instancias.
- **Sí:** invalidar sesiones activas anteriores durante el despliegue.
- **No:** entregar JWT o refresh tokens al navegador.
- **No:** usar `localStorage` o `sessionStorage` para credenciales.
- **No:** permitir CORS abierto.

## Qué no está en esta spec

- Auditoría estructurada, cubierta por SPEC 12.
- Cambio de contraseña, cubierto por SPEC 13.
- Recuperación o reset de contraseña.
- Clientes móviles y autenticación de integraciones externas.
