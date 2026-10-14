# SPEC 02 — Identidad, roles y JWT

> **Estado:** Borrador
> **Dependencias:** SPEC 01
> **Fecha:** 2026-10-13
> **Objetivo:** Incorporar Identity, roles, administrador inicial e inicio de sesión mediante JWT sin acoplar Application a ASP.NET Core.

## Alcance

**Incluye:**

- Modelo `User` y `UserId` en Domain.
- Caso de uso `LoginUser`.
- Puertos `IUserRepository`, `IUserCredentialService` e `ITokenIssuer`.
- ASP.NET Core Identity y EF Core en Infrastructure.
- Roles `Admin` y `User`.
- Seed idempotente del administrador.
- `POST /api/auth/login`.
- JWT con issuer, audience, firma y expiración validados.
- Política `AdminOnly`.
- Respuestas uniformes 401 y 403.
- Esquema Bearer en Swagger.
- Política configurable de contraseñas y bloqueo.

**Fuera de alcance:**

- Refresh tokens.
- Recuperación de contraseña.
- Registro público.
- Invitaciones y envío de correo.
- Permisos granulares del rol `User`.

## Contratos y configuración

```text
LoginRequest
- Email
- Password

LoginResponse
- AccessToken
- TokenType = "Bearer"
- ExpiresIn
```

Configuración requerida:

- `Jwt:Issuer`.
- `Jwt:Audience`.
- `Jwt:SigningKey`, siempre como secreto.
- `Jwt:AccessTokenLifetimeMinutes`, predeterminado en 60.
- `BootstrapAdmin:Email`, desde configuración segura.
- `BootstrapAdmin:Password`, desde un secreto.

Valores predeterminados de Identity:

- Longitud mínima de 12 caracteres.
- Mayúscula, minúscula, dígito y símbolo obligatorios.
- Seis caracteres únicos.
- Regex adicional opcional mediante `IPasswordValidator<TUser>`.
- Bloqueo de 15 minutos después de cinco intentos fallidos.
- La configuración se valida al iniciar.

## Plan de implementación

1. Crear el modelo independiente de usuario y los puertos de Application.
2. Implementar Identity y persistencia PostgreSQL en Infrastructure.
3. Implementar emisión de JWT detrás de `ITokenIssuer`.
4. Crear un seed idempotente y estricto para roles y administrador.
5. Implementar login, challenge 401, forbid 403 y política `AdminOnly`.
6. Documentar autenticación Bearer y respuestas 401/403 en Swagger.
7. Probar casos de uso y flujo HTTP contra PostgreSQL efímero.

## Criterios de aceptación

- [ ] El seed crea los roles `Admin` y `User` cuando no existen.
- [ ] El seed crea un administrador desde configuración segura y no sobrescribe su contraseña posteriormente.
- [ ] La aplicación falla al iniciar si la configuración obligatoria de seed, JWT o Identity es inválida.
- [ ] Credenciales válidas devuelven un JWT con rol, issuer, audience y expiración.
- [ ] Credenciales inválidas, usuario bloqueado o usuario sin contraseña devuelven el mismo 401 seguro.
- [ ] Un JWT expirado o con firma, issuer o audience incorrectos es rechazado.
- [ ] Un usuario `Admin` satisface `AdminOnly`.
- [ ] Un usuario `User` recibe 403 al acceder a operaciones administrativas.
- [ ] Los 401 y 403 contienen `code` y `traceId`.
- [ ] Swagger permite introducir el JWT mediante `Authorize`.
- [ ] Las pruebas de integración usan PostgreSQL efímero.

## Decisiones

- **Sí:** administrador inicial mediante seed seguro.
- **No:** todos los usuarios como administradores.
- **Sí:** access token de 60 minutos configurable.
- **No:** refresh token en esta fase.
- **Sí:** opciones nativas de Identity más un validador Regex opcional.
- **No:** reglas de contraseña dentro de controllers.

## Qué no está incluido en esta spec

- Registro de usuarios mediante endpoint.
- Activación por correo.
- Reenvío de invitaciones.
- Refresh tokens y recuperación de contraseña.
- Permisos funcionales del rol `User` sobre el balance.
