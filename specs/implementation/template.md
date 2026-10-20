# Implementar: 04-api-invitaciones-y-validacion-identidad

## PASOS

### 1.

---

### ?. Ajustes por pruebas manuales

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

### ?. Validación, ejecución local y Git

**Prerrequisitos:** Docker disponible; crear un archivo `.env` local a partir de `.env.example` y sustituir todos los valores de ejemplo por credenciales y secretos propios. **No subir `.env` a Git.**

**Iniciar API:**

```powershell
.\run-local.ps1
```

---

### ?. Validación del spec (flujo de trabajo existente):

```powershell
/spec-acceptance-validator @specs/04-api-invitaciones-y-validacion-identidad.md
```

Revisar el spec: [`04-api-invitaciones-y-validacion-identidad.md`](../04-api-invitaciones-y-validacion-identidad.md).

---

### ?. Diagrama Final: Arquitectura del spec-04-api-invitaciones-y-validacion-identidad

---

### ?. Comandos Git / PR\*\* (una vez validada la implementación y revisados los cambios)

```powershell
git add .
git commit -m "FEPP-ContaOps - spec-04-api-invitaciones-y-validacion-identidad -  Implementado"
git push -u origin spec-04-api-invitaciones-y-validacion-identidad

# Después de aprobar y fusionar el Pull Request:
git switch main
git pull
git branch -d spec-04-api-invitaciones-y-validacion-identidad
```
