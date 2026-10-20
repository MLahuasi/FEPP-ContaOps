# SPEC 07 — Eliminación de Data Protection y key ring

> **Estado:** Borrador
> **Dependencias:** SPEC 06
> **Fecha:** 2026-10-20
> **Objetivo:** Eliminar el uso operativo de ASP.NET Core Data Protection y toda configuración de key ring después de migrar las invitaciones.

## Alcance

**Incluye:**

- Inventario final de consumidores de Data Protection.
- Eliminación de `UserInvitationTokenProvider` basado en `DataProtectorTokenProvider`.
- Eliminación de `UserInvitationTokenProviderOptions` basada en Data Protection.
- Eliminación de `AddDataProtection`, persistencia de claves y DPAPI.
- Eliminación de `AddDefaultTokenProviders` si registra proveedores no requeridos.
- Eliminación de configuración, documentación y referencias obsoletas de key ring.
- Verificación de que Identity, JWT y correo continúan funcionando.

**Fuera de alcance:**

- Cambios al framework compartido `Microsoft.AspNetCore.App`.
- Cambios al algoritmo JWT.
- Sesiones, refresh tokens y auditoría.

## Plan de implementación

1. Ejecutar `docker compose ps` y confirmar que no se eliminarán volúmenes ni se reconfigurará pgAdmin.
2. Buscar todas las referencias de código, configuración, documentación y Docker a Data Protection y key ring.
3. Eliminar las clases y opciones específicas del proveedor Data Protection que ya no tengan consumidores.
4. Eliminar `AddDataProtection`, `PersistKeysToFileSystem`, `ProtectKeysWithDpapi` y los `using` asociados.
5. Reemplazar `AddDefaultTokenProviders` por el registro explícito de los proveedores realmente necesarios.
6. Eliminar `DataProtection__KeyRingPath` de `.env`, `.env.example`, README y AGENTS, sin modificar secretos reales.
7. Ejecutar búsqueda negativa y comprobar que solo queden menciones históricas justificadas en specs anteriores.
8. Ejecutar restore, build, pruebas unitarias, integración y verificación manual del arranque y Swagger.

## Criterios de aceptación

- [ ] No existe uso operativo de `IDataProtectionProvider` o `AddDataProtection`.
- [ ] No existe proveedor de invitación derivado de `DataProtectorTokenProvider`.
- [ ] No se crea ni persiste un key ring.
- [ ] La configuración vigente no contiene `DataProtection__KeyRingPath`.
- [ ] JWT Bearer continúa emitiendo y validando tokens.
- [ ] Las invitaciones continúan funcionando mediante `HashedUserTokenProvider`.
- [ ] Las pruebas completas pasan sin key ring.

## Decisiones

- **Sí:** eliminar el uso operativo, no intentar excluir ensamblados transitivos del framework.
- **Sí:** mantener Identity para usuarios, roles, contraseñas y proveedores explícitos.
- **No:** conservar Data Protection como compatibilidad temporal.

## Qué no está en esta spec

- Diseño de sesiones y refresh tokens.
- Auditoría de seguridad.
- Cambio de contraseña autenticado.
