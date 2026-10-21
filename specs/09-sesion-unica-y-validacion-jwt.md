# SPEC 09 — Sesión única y validación JWT

> **Estado:** Aprobado
> **Dependencias:** SPEC 08
> **Fecha:** 2026-10-20
> **Objetivo:** Vincular los JWT a una sesión persistida y revocarlos inmediatamente cuando la sesión o el SecurityStamp dejen de ser válidos.

## Alcance

**Incluye:**

- Claims `sid` y `jti` en JWT.
- Vigencia inicial de acceso de 15 minutos.
- Creación de sesión durante login.
- Revocación de la sesión anterior al iniciar una nueva.
- Validación de sesión contra PostgreSQL en cada request autenticado.
- Validación de usuario, stamp, estado y expiración.
- Rechazo uniforme con 401.
- Pruebas de equipos y sesiones sucesivas.

**Fuera de alcance:**

- Rotación HTTP de refresh tokens.
- Logout HTTP.
- Cambio de contraseña.
- Caching o Redis.

## Plan de implementación

1. Ejecutar `docker compose ps` y verificar que PostgreSQL y pgAdmin continúan activos.
2. Extender el contrato interno de emisión para incluir `SessionId` y `Jti`.
3. Configurar el JWT con `sid`, `jti`, expiración de 15 minutos, issuer y audience actuales.
4. Crear una sesión y revocar la sesión anterior después de validar el login.
5. Implementar validación de sesión persistida en el pipeline Bearer.
6. Rechazar sesiones inexistentes, revocadas, vencidas o con stamp diferente.
7. Mapear todos esos rechazos a `401 authentication_required` sin revelar la causa.
8. Probar login sucesivo desde dos equipos y confirmar que solo el último permanece activo.
9. Probar cambio de stamp y expiración de sesión con PostgreSQL real.
10. Ejecutar build, pruebas unitarias, integración y verificación de pgAdmin sin cambios de esquema.

## Criterios de aceptación

- [ ] Cada JWT contiene `sid` y `jti` únicos.
- [ ] Un nuevo login revoca la sesión anterior.
- [ ] Un JWT anterior recibe 401 en su siguiente request.
- [ ] La validación consulta PostgreSQL por request autenticado.
- [ ] Un cambio de SecurityStamp invalida todas las sesiones anteriores.
- [ ] La vigencia JWT es de 15 minutos.
- [ ] JWT Bearer conserva firma, issuer y audience válidos.

## Decisiones

- **Sí:** consulta directa a PostgreSQL para revocación inmediata.
- **Sí:** una única sesión activa por usuario.
- **No:** aceptar JWT únicamente por firma y expiración.
- **No:** agregar caché en esta fase.

## Qué no está en esta spec

- Rotación de refresh tokens.
- Endpoint de logout.
- Auditoría y cambio de contraseña.
