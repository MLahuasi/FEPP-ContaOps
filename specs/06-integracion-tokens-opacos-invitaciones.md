# SPEC 06 — Integración de tokens opacos con invitaciones

> **Estado:** Aprobado
> **Dependencias:** SPEC 05
> **Fecha:** 2026-10-20
> **Objetivo:** Sustituir los tokens Data Protection de invitación por un proveedor de Identity basado en tokens opacos hasheados y consumidos una sola vez.

## Alcance

**Incluye:**

- `HashedUserTokenProvider` registrado mediante `AddTokenProvider`.
- Uso de `UserManager.GenerateUserTokenAsync` y `VerifyUserTokenAsync` como extensión de Identity.
- Generación en creación y reenvío de invitaciones.
- Rotación de `SecurityStamp` al reenviar.
- Consumo atómico durante activación.
- Preservación del token cuando falla la validación de contraseña.
- Invalidación de tokens emitidos por la implementación anterior.
- Pruebas unitarias, de integración y de concurrencia.

**Fuera de alcance:**

- Eliminar aún las referencias globales a Data Protection.
- Sesiones y refresh tokens.
- Cambio autenticado de contraseña.
- Cambios al contrato público de las respuestas existentes.

## Flujo de tokens

- **Crear:** genera 32 bytes, guarda el hash con propósito `UserActivation` y envía el token original solo al notificador.
- **Reenviar:** rota el `SecurityStamp`, reemplaza la fila activa y emite un token nuevo.
- **Activar:** valida usuario pendiente, propósito, stamp, expiración y hash; consume la fila de forma atómica.
- **Fallo de contraseña:** revierte la transacción y conserva el token válido.
- **Éxito:** establece la contraseña, activa la cuenta, confirma email y rota el stamp.

## Plan de implementación

1. Verificar que PostgreSQL y pgAdmin existentes siguen activos con `docker compose ps`, sin eliminar volúmenes.
2. Implementar `HashedUserTokenProvider` con la abstracción de proveedor de tokens de Identity.
3. Registrar únicamente el proveedor dedicado bajo `IdentityUserInvitationService.TokenProviderName`.
4. Adaptar el registro de cuenta pendiente para guardar y devolver internamente el nuevo token.
5. Adaptar el reenvío para rotar stamp y reemplazar la fila anterior.
6. Adaptar la activación para validar y consumir el token dentro de la transacción.
7. Garantizar que tokens Data Protection anteriores sean inválidos y deban reemplazarse mediante reenvío.
8. Agregar pruebas de token válido, inválido, vencido, usado, de otro usuario y de reenvío.
9. Agregar prueba de dos activaciones concurrentes y comprobar que solo una tiene éxito.
10. Ejecutar build, unitarias, integración con Testcontainers y revisar que ninguna respuesta o log exponga el token.

## Criterios de aceptación

- [ ] Identity usa el proveedor dedicado para invitaciones.
- [ ] Crear y reenviar invitaciones emiten tokens opacos nuevos.
- [ ] El token anterior queda inválido después del reenvío.
- [ ] Un token consumido no puede reutilizarse.
- [ ] Dos activaciones concurrentes no pueden completar ambas.
- [ ] Una contraseña inválida no consume la invitación.
- [ ] Los tokens anteriores a la refactorización se invalidan y requieren reenvío.
- [ ] JWT Bearer permanece sin cambios.

## Decisiones

- **Sí:** conservar `UserManager` como fachada de Identity.
- **Sí:** un propósito explícito permite reutilizar el proveedor en futuros flujos.
- **Sí:** invalidar tokens antiguos; no se conserva compatibilidad temporal con Data Protection.
- **No:** exponer `TokenId`, hash o stamp en HTTP.

## Qué no está en esta spec

- Limpieza final de Data Protection y key ring.
- Sesiones persistentes y refresh tokens.
- Endpoint de cambio de clave.
