# SPEC 05 — Persistencia y criptografía de tokens opacos

> **Estado:** Aceptado
> **Dependencias:** SPEC 03, SPEC 04
> **Fecha:** 2026-10-20
> **Objetivo:** Crear el núcleo persistente y criptográfico reutilizable para almacenar hashes de tokens opacos sin depender de ASP.NET Core Data Protection.

## Alcance

**Incluye:**

- Entidad de Infrastructure `HashedUserToken`.
- Tabla `UserSecurityTokens` con una fila activa por usuario y propósito.
- Generación con `RandomNumberGenerator` de 32 bytes.
- Representación pública Base64Url.
- Hash SHA-256 y comparación en tiempo constante.
- `TokenId`, `SecurityStamp`, `CreatedAtUtc` y `ExpiresAtUtc`.
- Almacenamiento y consulta reutilizables por propósito.
- Pruebas unitarias de generación, hash, expiración y unicidad.
- Reinicio local desde cero mediante Docker y verificación inicial en pgAdmin.

**Fuera de alcance:**

- Sustituir todavía el flujo de invitaciones existente.
- Sesiones, JWT, refresh tokens y logout.
- Endpoint de cambio de contraseña.
- Eliminación final de Data Protection.
- Migraciones o actualización in-place de bases existentes.

## Modelo de datos

`UserSecurityTokens` contiene como mínimo:

- `TokenId` (`uuid`) como clave primaria.
- `UserId` (`string`) como clave foránea a `AspNetUsers`.
- `Purpose` (`string`).
- `TokenHash` (`bytea` de 32 bytes).
- `SecurityStamp` (`string`).
- `CreatedAtUtc` (`timestamp with time zone`).
- `ExpiresAtUtc` (`timestamp with time zone`).

Debe existir un índice único sobre `(UserId, Purpose)` y borrado en cascada con el usuario. El `TokenId` es interno y nunca se envía por HTTP, correo o logs.

## Plan de implementación

1. Ejecutar `docker compose down --volumes` para eliminar los datos locales de PostgreSQL y pgAdmin.
2. Ejecutar `docker compose up -d database pgadmin` y comprobar `docker compose ps`.
3. Registrar en pgAdmin el servidor `FEPP-ContaOps Local` usando host `database`, puerto `5432`, base `fepp_contaops_dev`, usuario `app` y la contraseña de `POSTGRES_PASSWORD`.
4. Crear `HashedUserToken` y configurar `UserSecurityTokens` en el modelo de Infrastructure.
5. Registrar la relación, índice único, tipos PostgreSQL y borrado en cascada.
6. Implementar la generación criptográficamente segura y la codificación Base64Url.
7. Implementar el cálculo SHA-256, la comparación constante y la validación de propósito, stamp y expiración.
8. Implementar el almacenamiento de una sola fila activa, reemplazo y eliminación de filas vencidas.
9. Agregar pruebas unitarias para aleatoriedad, hash no reversible, expiración, propósito y comparación inválida.
10. Ejecutar build y pruebas, iniciar la API para crear el esquema y verificar la tabla y restricciones desde pgAdmin.

## Criterios de aceptación

- [ ] El token público contiene 32 bytes aleatorios y usa Base64Url.
- [ ] La base de datos nunca almacena el token original.
- [ ] La comparación usa una operación de tiempo constante.
- [ ] Solo existe una fila activa por `(UserId, Purpose)`.
- [ ] El token vencido se rechaza y puede eliminarse oportunistamente.
- [ ] El modelo no introduce Data Protection.
- [ ] La tabla aparece en PostgreSQL después de iniciar una base nueva.
- [ ] Las pruebas unitarias cubren los casos criptográficos y temporales.

## Decisiones

- **Sí:** tabla dedicada `UserSecurityTokens`; permite separar tokens funcionales de Identity.
- **Sí:** SHA-256 sobre entradas de alta entropía; no se requiere un secreto de servidor adicional.
- **Sí:** `TokenId`, `SecurityStamp` y fechas como metadatos operativos.
- **No:** guardar tokens planos o agregar historial en esta fase.
- **No:** crear migraciones; el entorno local se recrea desde cero.

## Qué no está en esta spec

- Integración del proveedor con invitaciones.
- Refresh tokens y sesiones.
- Auditoría y cambio de contraseña.
