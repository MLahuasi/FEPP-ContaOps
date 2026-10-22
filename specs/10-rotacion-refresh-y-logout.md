# SPEC 10 — Rotación de refresh tokens y logout

> **Estado:** Aprobado
> **Dependencias:** SPEC 09
> **Fecha:** 2026-10-20
> **Objetivo:** Permitir renovar sesiones sin repetir credenciales y cerrar sesiones invalidando inmediatamente sus tokens.

## Alcance

**Incluye:**

- Emisión de refresh token en login.
- `POST /api/auth/refresh`.
- Rotación atómica de refresh token.
- Detección de reutilización y revocación de sesión.
- `POST /api/auth/logout`.
- Vigencia máxima de sesión de 7 días.
- Respuestas JSON explícitas.
- Pruebas de concurrencia y seguridad.

**Fuera de alcance:**

- Cookies, CORS, CSRF y frontend.
- Múltiples sesiones por usuario.
- Cambio de contraseña.
- Nuevos algoritmos de firma JWT.

## Contratos

`POST /api/auth/refresh` recibe `refreshToken` y devuelve `accessToken`, `tokenType`, `expiresIn` y `refreshToken`. `POST /api/auth/logout` requiere el Bearer actual y devuelve `204`. Los tokens no aparecen en logs ni errores.

## Plan de implementación

1. Ejecutar `docker compose ps` y verificar servicios y pgAdmin sin eliminar volúmenes.
2. Incluir el refresh token en la respuesta de login y limitar la sesión a siete días.
3. Implementar la búsqueda y validación del hash, sesión, stamp y expiración.
4. Consumir atómicamente el refresh presentado y crear el reemplazo.
5. Detectar reutilización de un token consumido y revocar la sesión completa.
6. Implementar `POST /api/auth/refresh` con errores neutrales.
7. Implementar `POST /api/auth/logout` y revocación persistente.
8. Probar refresh concurrente, reutilización, expiración y logout.
9. Verificar que los JWT anteriores dejan de funcionar mediante la validación de sesión.
10. Ejecutar build, pruebas completas, Swagger y comprobación de tablas en pgAdmin.

## Criterios de aceptación

- [ ] Login devuelve JWT y refresh token.
- [ ] El refresh anterior queda inutilizable tras rotar.
- [ ] Dos refresh concurrentes no generan dos sesiones válidas.
- [ ] Reutilizar un refresh consumido revoca la sesión.
- [ ] Logout invalida refresh y JWT inmediatamente.
- [ ] La sesión no puede superar siete días.
- [ ] Las respuestas no contienen hashes ni identificadores internos innecesarios.

## Decisiones

- **Sí:** refresh token en JSON; el cliente debe almacenarlo de forma segura.
- **Sí:** rotación obligatoria y detección de reutilización.
- **No:** guardar el token plano.
- **No:** usar cookies sin definir el frontend.

## Qué no está en esta spec

- Cambio de contraseña.
- Auditoría detallada de operaciones.
