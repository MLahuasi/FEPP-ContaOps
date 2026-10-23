# SPEC 12 — Auditoría de sesiones y credenciales

> **Estado:** Borrador
> **Dependencias:** SPEC 04, SPEC 11
> **Fecha:** 2026-10-20
> **Objetivo:** Añadir trazabilidad estructurada de sesiones web, accesos y operaciones de credenciales sin registrar información confidencial.

## Alcance

**Incluye:**

- Eventos estructurados en el sink JSON de Serilog existente.
- `EventId` y nombres estables de evento.
- Éxito y rechazo de login BFF.
- Éxito y rechazo de logout.
- Rechazo, expiración y revocación de sesiones.
- Rechazo de solicitudes por CSRF.
- Éxito y rechazo de activación/primera contraseña.
- Preparación de eventos de cambio de contraseña para SPEC 13.
- `UserId`, `SessionId`, `TokenId` y `TraceId` solo cuando sean necesarios.
- Advertencias neutrales para fallos esperados.
- Errores inesperados registrados una sola vez por `GlobalExceptionHandler`.
- Pruebas de redacción.

**Fuera de alcance:**

- Tabla de auditoría.
- Cambiar el formato o destino configurado en `appsettings.json`.
- Crear el endpoint de cambio de contraseña.
- Registrar emails, IP o User-Agent.

## Catálogo inicial

- `security.login.succeeded` — Information.
- `security.login.rejected` — Warning.
- `security.logout.succeeded` — Information.
- `security.session.rejected` — Warning.
- `security.session.expired` — Information.
- `security.csrf.rejected` — Warning.
- `security.initial_password.succeeded` — Information.
- `security.initial_password.rejected` — Warning.
- `security.password_change.succeeded` y `security.password_change.rejected` quedan preparados para SPEC 13.
- `application.unhandled_exception` — Error, con excepción, ruta y `TraceId`.

## Plan de implementación

1. Ejecutar `docker compose ps`; no eliminar volúmenes ni reconfigurar pgAdmin.
2. Definir constantes `EventId` y nombres estables en un componente reutilizable.
3. Definir las propiedades permitidas y la política de redacción.
4. Auditar éxito y rechazo de login y logout BFF.
5. Auditar rechazo, expiración y revocación de sesiones.
6. Auditar activación y preparar eventos para el cambio de contraseña de SPEC 13.
7. Ajustar el handler global para evitar duplicación de excepciones.
8. Probar estructura, niveles y ausencia de secretos en los eventos.
9. Verificar los archivos JSON rotatorios y ejecutar build y pruebas.

## Criterios de aceptación

- [ ] Los eventos aparecen en el JSON de Serilog.
- [ ] Cada evento tiene `EventId`, operación, resultado y `TraceId`.
- [ ] Los rechazos esperados usan `Warning` neutral.
- [ ] Los fallos internos conservan excepción y stack trace solo en logs.
- [ ] Nunca se registran contraseñas, JWT, refresh tokens, hashes, stamps, emails o cuerpos HTTP.
- [ ] No se crea una tabla de auditoría.
- [ ] Los eventos de login, logout, sesión y primera contraseña tienen pruebas.

## Decisiones

- **Sí:** reutilizar el logging JSON existente.
- **Sí:** permitir `UserId`, `SessionId` y `TokenId` como identificadores no secretos.
- **No:** registrar PII adicional, como email, IP o User-Agent.
- **No:** duplicar el error técnico ya registrado por el handler global.

## Qué no está en esta spec

- Endpoint de cambio de contraseña; solo se preparan sus eventos para SPEC 13.
- Persistencia histórica en base de datos.
