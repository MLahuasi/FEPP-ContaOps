# SPEC 08 — Persistencia de sesiones y refresh tokens

> **Estado:** Borrador
> **Dependencias:** SPEC 07
> **Fecha:** 2026-10-20
> **Objetivo:** Crear el almacenamiento seguro de una sesión por usuario y de refresh tokens rotatorios sin persistir JWT completos.

## Alcance

**Incluye:**

- Entidades `UserSession` y `UserRefreshToken`.
- Tablas `UserSessions` y `UserRefreshTokens`.
- `SessionId`, `SecurityStamp`, estado, creación y expiración.
- Hashes de refresh tokens de 32 bytes.
- Una sesión activa por usuario.
- Un refresh token utilizable por sesión.
- Detección de consumo y reutilización.
- Reinicio de Docker y pgAdmin para crear el esquema desde cero.

**Fuera de alcance:**

- Validación del JWT por request.
- Endpoints refresh y logout.
- Cambio de contraseña.
- Tabla de auditoría.

## Modelo de datos

`UserSessions` debe contener usuario, `SessionId`, stamp, estado, `CreatedAtUtc`, `ExpiresAtUtc`, `RevokedAtUtc` y motivo seguro. `UserRefreshTokens` debe contener `TokenId`, `SessionId`, hash, creación, expiración y consumo/revocación. Los hashes nunca se exponen.

## Plan de implementación

1. Ejecutar `docker compose down --volumes` para recrear PostgreSQL y pgAdmin desde cero.
2. Ejecutar `docker compose up -d database pgadmin` y verificar `docker compose ps`.
3. Registrar nuevamente en pgAdmin `FEPP-ContaOps Local` con host `database`, puerto `5432`, base `fepp_contaops_dev`, usuario `app` y `POSTGRES_PASSWORD`.
4. Crear las entidades de sesión y refresh token en Infrastructure.
5. Configurar relaciones, índices, unicidad por usuario y borrado en cascada.
6. Implementar generación aleatoria, hash y comparación de refresh tokens.
7. Implementar almacenamiento de una sesión activa y un refresh activo por usuario.
8. Implementar estados de consumo, revocación y expiración sin guardar tokens planos.
9. Agregar pruebas unitarias de persistencia, unicidad, hash y reutilización.
10. Iniciar la API, verificar ambas tablas en pgAdmin y ejecutar build y pruebas.

## Criterios de aceptación

- [ ] Un usuario tiene como máximo una sesión activa.
- [ ] Una sesión tiene como máximo un refresh token utilizable.
- [ ] El JWT completo no se persiste.
- [ ] Solo se persisten hashes de refresh tokens.
- [ ] La revocación conserva trazabilidad suficiente sin secretos.
- [ ] Las tablas y restricciones se crean en una base nueva.

## Decisiones

- **Sí:** una sesión por usuario.
- **Sí:** persistir sesión y hash del refresh token.
- **Sí:** eliminar los datos locales y recrear el esquema en lugar de migrar in-place.
- **No:** usar `AspNetUserTokens` para sesiones.

## Qué no está en esta spec

- Endpoint de refresh, logout o cambio de clave.
- Auditoría estructurada.
