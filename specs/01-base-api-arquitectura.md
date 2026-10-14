# SPEC 01 — Base arquitectónica de la Web API

> **Estado:** Implementado
> **Dependencias:** Ninguna
> **Fecha:** 2026-10-13
> **Objetivo:** Crear la solución por capas y un límite HTTP uniforme con validación, Swagger, logging estructurado y respuestas `ProblemDetails` seguras.

## Alcance

**Incluye:**

- `FEPP-ContaOps.slnx` con Domain, Application, Infrastructure y Api.
- Referencias orientadas hacia Domain.
- DTOs HTTP y validadores en Api.
- Errores tipados de Application para validación, recurso inexistente y conflicto.
- `GlobalExceptionHandler` con `IExceptionHandler`.
- `AddExceptionHandler`, `AddProblemDetails` y `UseExceptionHandler` al inicio del pipeline.
- Respuestas con `type`, `title`, `status`, `detail`, `instance`, `code`, `traceId` y `errors` para validación.
- Logging JSON estructurado.
- Swagger configurable.
- Proyectos de pruebas unitarias y de integración.

**Fuera de alcance:**

- Modelo de regionales y balance global.
- Autenticación y usuarios.
- Importación de archivos.

## Responsabilidades por proyecto

- **Domain:** modelo e invariantes sin dependencias externas.
- **Application:** casos de uso, puertos y resultados tipados sin conceptos HTTP.
- **Infrastructure:** implementaciones técnicas y composición propia.
- **Api:** controllers, DTOs, FluentValidation, mapeo HTTP, Swagger y raíz de composición.

## Contrato de errores

| Situación                        | HTTP | Código               |
| -------------------------------- | ---: | -------------------- |
| DTO o precondición inválida      |  400 | `validation_error`   |
| Recurso inexistente              |  404 | `resource_not_found` |
| Duplicidad o estado incompatible |  409 | `resource_conflict`  |
| Excepción inesperada             |  500 | `unexpected_error`   |

Los errores esperados se traducen desde resultados tipados. El handler central registra excepciones inesperadas una sola vez y nunca devuelve mensajes internos, stack traces, rutas, consultas, secretos ni datos contables.

## Plan de implementación

1. Crear la solución, los cuatro proyectos productivos y los dos proyectos de pruebas.
2. Configurar referencias y extensiones de inyección de dependencias.
3. Definir resultados y errores tipados en Application.
4. Incorporar validación explícita de DTOs y traducción uniforme a `ProblemDetails`.
5. Configurar `GlobalExceptionHandler`, logging JSON y correlación por `traceId`.
6. Configurar Swagger mediante `Swagger:Enabled`; quedará deshabilitado por defecto en producción.
7. Agregar pruebas unitarias del mapeo y pruebas HTTP del pipeline.

## Criterios de aceptación

- [x] Domain no referencia ningún otro proyecto.
- [x] Api referencia Application e Infrastructure y actúa como raíz de composición.
- [x] Ningún controller expone entidades de Domain o Infrastructure.
- [x] Los validadores se ejecutan antes del caso de uso.
- [x] Las respuestas 400, 404, 409 y 500 usan el contrato `ProblemDetails` definido.
- [x] Cada respuesta de error contiene el mismo `traceId` utilizado por el log.
- [x] Una excepción inesperada se registra exactamente una vez en nivel `Error`.
- [x] Los errores esperados se registran estructuradamente en nivel `Information` o `Warning`.
- [x] Una cancelación solicitada por el cliente no se presenta como 500.
- [x] Swagger puede activarse por configuración y queda deshabilitado por defecto en producción.
- [x] Las pruebas verifican que las respuestas no filtren detalles internos.

## Decisiones

- **Sí:** resultados tipados para fallos esperados.
- **No:** excepciones como flujo normal de Application.
- **Sí:** FluentValidation explícito; registrar el paquete no activa la validación por sí solo.
- **Sí:** protección productiva de Swagger mediante proxy, VPN o allowlist cuando se habilite.
- **No:** proteger la propia UI únicamente con el botón `Authorize`.

## Qué no está incluido en esta spec

- Usuarios, roles, registro e inicio de sesión.
- JWT y autorización.
- Invitaciones y envío de correo.
- Operaciones del balance global.
