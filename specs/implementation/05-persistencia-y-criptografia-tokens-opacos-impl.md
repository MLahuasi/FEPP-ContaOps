# Implementar: 05-persistencia-y-criptografia-tokens-opacos

## PASOS

### 1. Reiniciar los volúmenes locales de Docker y arrancar database y pgadmin.

---

### 2. Comprobar el estado de los contenedores.

---

### 3. Registrar/verificar el servidor en pgAdmin.

---

### 4. Crear HashedUserToken y configurar UserSecurityTokens.

---

### 5. Configurar relación, índice único, tipos PostgreSQL y cascada.

---

### 6. Implementar generación segura y Base64Url.

---

### 7. Implementar hash SHA-256, comparación constante y validaciones.

---

### 8. Implementar almacenamiento, reemplazo y eliminación de vencidos.

---

### 9. Agregar pruebas unitarias.

---

### 10. Ejecutar build, pruebas, API y verificación del esquema.

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
/spec-acceptance-validator @specs/05-persistencia-y-criptografia-tokens-opacos.md
```

Revisar el spec: [`05-persistencia-y-criptografia-tokens-opacos.md`](../05-persistencia-y-criptografia-tokens-opacos.md).

---

### 14. Diagrama Final: Arquitectura del spec-05-persistencia-y-criptografia-tokens-opacos

---

### 15. Comandos Git / PR\*\* (una vez validada la implementación y revisados los cambios)

```powershell
git add .
git commit -m "FEPP-ContaOps - spec-05-persistencia-y-criptografia-tokens-opacos -  Implementado"
git push -u origin spec-05-persistencia-y-criptografia-tokens-opacos

# Después de aprobar y fusionar el Pull Request:
git switch main
git pull
git branch -d spec-05-persistencia-y-criptografia-tokens-opacos
```
