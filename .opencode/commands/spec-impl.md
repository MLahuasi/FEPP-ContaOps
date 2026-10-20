---
description: Implementa una spec aprobada siguiendo el flujo spec-impl
agent: build
---

Carga y sigue íntegramente la skill `.agents/skills/spec-impl/SKILL.md`.

Implementa la spec indicada en `$ARGUMENTS`.

Respeta estrictamente el flujo de la skill:

- valida que la spec esté en estado `Aprobado` o equivalente;
- comprueba el estado del árbol de trabajo antes de cambiar de rama;
- crea o selecciona la rama correspondiente según `specs/.spec-config.yml`;
- muestra el resumen de objetivo, alcance, plan y criterios de aceptación;
- solicita confirmación antes de comenzar;
- implementa un paso cada vez y pausa para revisar el diff;
- no crea commits automáticamente;
- no modifica specs históricas ni archivos fuera del alcance aprobado;
- al finalizar, verifica los criterios de aceptación con evidencia ejecutable.
