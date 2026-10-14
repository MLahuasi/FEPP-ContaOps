# Plan de implementación del balance global

Este documento divide la implementación en fases amplias. Cada prompt está pensado para solicitar a OpenCode la planificación de una especificación antes de implementar. Ejecutar las fases en orden y marcar cada paso cuando la implementación correspondiente esté completada.

Usar como contexto `global-balance-sheet.md`, `webapi-architecture.md`, `README.md` y `dotnet-naming-and-code-documentation.md`.

## Fase 1: Base de API y autenticación

| Paso | Título | Prompt | Check |
|---|---|---|---|
| 1 | Preparar la API y el acceso autenticado | Analiza la arquitectura documentada y planifica la base necesaria para exponer el módulo de balance global: organización por capas, composición de dependencias, contratos DTO y validaciones. Incluye registro e inicio de sesión con JWT, Swagger y manejo centralizado de excepciones con `IExceptionHandler`, `UseExceptionHandler` y respuestas `ProblemDetails` seguras, según `webapi-architecture.md`. Pide mapear errores de validación, recurso inexistente, conflicto y excepción inesperada, con logging y `traceId`. Entrega una spec breve con objetivo, responsabilidades por proyecto y criterios de aceptación. No implementes cambios todavía. | [ ] |

## Fase 2: Regionales y planes de cuentas

| Paso | Título | Prompt | Check |
|---|---|---|---|
| 2 | Administrar regionales y asociar archivos | Con base en `global-balance-sheet.md`, planifica la gestión de regionales y el registro que relaciona el nombre de cada archivo con su regional. Considera estados activo e inactivo, regionales sin planes asociados y la unicidad de nombres de archivo activos. Define operaciones y validaciones necesarias sin inventar campos fuera del modelo documentado. Entrega objetivo, alcance, capas y criterios de aceptación; no escribas código. | [ ] |

## Fase 3: Lectura y jerarquía de cuentas

| Paso | Título | Prompt | Check |
|---|---|---|---|
| 3 | Interpretar archivos PDF y Excel | Planifica la lectura de los formatos PDF y Excel indicados en `global-balance-sheet.md`. La spec debe cubrir cómo reconocer filas de cuentas, ignorar títulos y encabezados, extraer código y nombre, calcular nivel y cuenta padre, y reportar errores con archivo y línea o fila. Señala qué ejemplos de archivos serían necesarios para validar formatos reales. No implementes código. | [ ] |

## Fase 4: Importación e historial

| Paso | Título | Prompt | Check |
|---|---|---|---|
| 4 | Importar cuentas con consistencia por archivo | Planifica la persistencia de cuentas siguiendo las reglas de `global-balance-sheet.md`: una transacción por archivo, desactivar las cuentas anteriores de la regional sin borrarlas, insertar el nuevo plan como activo, impedir códigos activos repetidos y revertir por completo el archivo que falle. La falla de un archivo no debe impedir procesar los siguientes. Entrega una spec con responsabilidades de Application, Domain e Infrastructure y criterios de aceptación. No escribas código. | [ ] |

## Fase 5: Recorrido de archivos y reporte

| Paso | Título | Prompt | Check |
|---|---|---|---|
| 5 | Procesar el directorio Plan_Cuentas | Diseña la planificación del proceso que recorre el directorio raíz `Plan_Cuentas`, configurado mediante User Secrets como `FileImport:RootPath`. Debe buscar los archivos registrados con estado activo, resolver su regional, validar regional y formato, procesar cada archivo de forma independiente y continuar ante errores. Define el reporte final con archivos encontrados, correctos y fallidos, cuentas importadas y detalle de errores. Propón una forma de iniciar el proceso desde la Web API compatible con la arquitectura existente. No implementes código. | [ ] |

## Fase 6: Integración y revisión funcional

| Paso | Título | Prompt | Check |
|---|---|---|---|
| 6 | Integrar el módulo y comprobar criterios | Revisa las specs previas y planifica su integración en la Web API. Verifica que los DTOs y validaciones estén en Api, que Application coordine los casos de uso, que Domain proteja las reglas y que Infrastructure implemente persistencia y lectura de archivos. Incluye autorización JWT y manejo centralizado de excepciones conforme a `webapi-architecture.md`: respuestas ProblemDetails seguras, mapeo de errores conocidos, registro único de excepciones inesperadas y correlación por `traceId`. Verifica que los errores de un archivo se agreguen al reporte y permitan continuar el lote, mientras que una falla global llegue al handler HTTP. Los criterios de aceptación deben cubrir regionales, jerarquía, duplicados, rollback por archivo y reporte final. Identifica inconsistencias o decisiones pendientes antes de implementar. No escribas código. | [ ] |

## Criterio de avance

Cada paso puede tratarse como una spec independiente. Antes de implementar, revisar que OpenCode haya producido alcance y criterios de aceptación claros; resolver decisiones pendientes con el contexto del proyecto y actualizar la spec. Marcar el paso cuando su implementación esté completada.