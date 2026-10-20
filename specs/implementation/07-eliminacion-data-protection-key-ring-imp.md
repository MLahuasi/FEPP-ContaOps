# Implementar: 07-eliminacion-data-protection-key-ring

## PASOS

### 1. Ejecutar docker compose ps y confirmar que no se eliminarán volúmenes ni se reconfigurará pgAdmin.

---

### 2. Buscar referencias de código, configuración, documentación y Docker.

---

### 3. Eliminar clases/opciones específicas de Data Protection sin consumidores.

---

### 4. Eliminar registros de Data Protection, persistencia, DPAPI y using asociados.

---

### 5. Registrar explícitamente solo los proveedores de Identity necesarios.

---

### 6. Eliminar DataProtection\_\_KeyRingPath de configuración/documentación, sin modificar secretos reales.

---

### 7. Ejecutar búsqueda negativa y justificar menciones históricas.

---

### 8. Ejecutar restore, build, pruebas y verificaciones manuales.

---

### 9. Ajustes por pruebas manuales

**Levantar PostgreSQL y pgAdmin:**

```powershell
docker compose up -d
```

**Compilación y pruebas:**

```powershell
dotnet restore FEPP-ContaOps.slnx
dotnet build FEPP-ContaOps.slnx
dotnet test FEPP-ContaOps.slnx
```

---

### 10. Validación, ejecución local y Git

**Prerrequisitos:** Docker disponible; crear un archivo `.env` local a partir de `.env.example` y sustituir todos los valores de ejemplo por credenciales y secretos propios. **No subir `.env` a Git.**

**Iniciar API:**

```powershell
.\run-local.ps1
```

---

### 11. Validación del spec (flujo de trabajo existente):

```powershell
/spec-acceptance-validator @specs/07-eliminacion-data-protection-key-ring.md
```

Revisar el spec: [`07-eliminacion-data-protection-key-ring.md`](../07-eliminacion-data-protection-key-ring.md).

---

### 12. Diagrama Final: Arquitectura del spec-07-eliminacion-data-protection-key-ring

---

### 13. Comandos Git / PR\*\* (una vez validada la implementación y revisados los cambios)

```powershell
git add .
git commit -m "FEPP-ContaOps - spec-07-eliminacion-data-protection-key-ring -  Implementado"
git push -u origin spec-07-eliminacion-data-protection-key-ring

# Después de aprobar y fusionar el Pull Request:
git switch main
git pull
git branch -d spec-07-eliminacion-data-protection-key-ring
```
