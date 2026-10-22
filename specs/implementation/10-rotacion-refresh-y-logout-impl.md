# Implementar: 10-rotacion-refresh-y-logout

## PASOS

### 1. Verificar servicios con docker compose ps.

---

### 2. Añadir refresh token al login y limitar la sesión a 7 días.

---

### 3. Validar hash, sesión, stamp y expiración.

---

### 4. Consumir atómicamente el refresh y crear su reemplazo.

---

### 5. Detectar reutilización y revocar la sesión.

---

### 6. Implementar POST /api/auth/refresh.

---

### 7. Implementar POST /api/auth/logout.

---

### 8. Probar concurrencia, reutilización, expiración y logout.

---

### 9. Verificar la invalidación de JWT anteriores.

---

### 10. Ejecutar build, pruebas, Swagger y comprobación de tablas.

---

### 11. Ajustes por pruebas manuales

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

### 12. Validación, ejecución local y Git

**Prerrequisitos:** Docker disponible; crear un archivo `.env` local a partir de `.env.example` y sustituir todos los valores de ejemplo por credenciales y secretos propios. **No subir `.env` a Git.**

**Iniciar API:**

```powershell
.\run-local.ps1
```

---

### 13. Validación del spec (flujo de trabajo existente):

```powershell
/spec-acceptance-validator @specs/10-rotacion-refresh-y-logout.md
```

Revisar el spec: [`10-rotacion-refresh-y-logout.md`](../10-rotacion-refresh-y-logout.md).

---

### 14. Diagrama Final: Arquitectura del spec-10-rotacion-refresh-y-logout

---

### 15. Comandos Git / PR\*\* (una vez validada la implementación y revisados los cambios)

```powershell
git add .
git commit -m "FEPP-ContaOps - spec-10-rotacion-refresh-y-logout -  Implementado"
git push -u origin spec-10-rotacion-refresh-y-logout

# Después de aprobar y fusionar el Pull Request:
git switch main
git pull
git branch -d spec-10-rotacion-refresh-y-logout
```
