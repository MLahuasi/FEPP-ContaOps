# SPEC 11 — Auditoría de login y credenciales

> **Estado:** Borrador
> **Dependencias:** SPEC 04, SPEC 10
> **Fecha:** 2026-10-20
> **Objetivo:** Añadir trazabilidad estructurada de accesos y operaciones de contraseña sin registrar información confidencial.

## Alcance

**Incluye:**

- Eventos estructurados en el sink JSON de Serilog existente.
- `EventId` y nombres estables de evento.
- Éxito y rechazo de login.
- Éxito y rechazo de activación/primera contraseña.
- Preparación del evento de cambio de contraseña para SPEC 12.
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
- `security.initial_password.succeeded` — Information.
- `security.initial_password.rejected` — Warning.
- `security.password_change.succeeded` y `security.password_change.rejected` quedan preparados para SPEC 12.
- `application.unhandled_exception` — Error, con excepción, ruta y `TraceId`.

## Plan de implementación

1. Ejecutar `docker compose ps`; no eliminar volúmenes ni reconfigurar pgAdmin.
2. Definir constantes `EventId` y nombres de eventos en un componente reutilizable.
3. Implementar logging estructurado mediante `ILogger` con las propiedades permitidas.
4. Registrar éxito y rechazo del login con mensajes neutrales.
5. Registrar éxito y rechazo de la primera contraseña durante activación.
6. Ajustar el handler global para identificar errores internos sin duplicar excepciones.
7. Agregar pruebas que inspeccionen eventos y aseguren ausencia de secretos.
8. Verificar los archivos JSON rotatorios configurados por `appsettings.json`.
9. Ejecutar build, pruebas unitarias, integración y documentar el catálogo.

## Criterios de aceptación

- [ ] Los eventos aparecen en el JSON de Serilog.
- [ ] Cada evento tiene `EventId`, operación, resultado y `TraceId`.
- [ ] Los rechazos esperados usan `Warning` neutral.
- [ ] Los fallos internos conservan excepción y stack trace solo en logs.
- [ ] Nunca se registran contraseñas, JWT, refresh tokens, hashes, stamps, emails o cuerpos HTTP.
- [ ] No se crea una tabla de auditoría.
- [ ] Los eventos de login y primera contraseña tienen pruebas.

## Decisiones

- **Sí:** reutilizar el logging JSON existente.
- **Sí:** permitir `UserId`, `SessionId` y `TokenId` como identificadores no secretos.
- **No:** registrar PII adicional, como email, IP o User-Agent.
- **No:** duplicar el error técnico ya registrado por el handler global.

## Qué no está en esta spec

- Endpoint de cambio de contraseña.
- Persistencia histórica en base de datos.
