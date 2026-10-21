# Implementar: 09-sesion-unica-y-validacion-jwt

## PASOS

### 1. Verificar docker compose ps.

---

### 2. Extender el contrato de emisión con SessionId y Jti.

---

### 3. Configurar claims, expiración de 15 minutos, issuer y audience.

---

### 4. Crear sesión y revocar la anterior tras validar login.

---

### 5. Validar sesión persistida en Bearer.

---

### 6. Rechazar sesiones inexistentes, revocadas, vencidas o con stamp diferente.

---

### 7. Mapear rechazos a 401 authentication_required.

---

### 8. Probar login sucesivo desde dos equipos.

---

### 9. Probar cambio de stamp y expiración con PostgreSQL real.

---

### 10. Ejecutar build, pruebas y verificación de pgAdmin sin cambios de esquema.

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
/spec-acceptance-validator @specs/09-sesion-unica-y-validacion-jwt.md
```

Revisar el spec: [`09-sesion-unica-y-validacion-jwt.md`](../09-sesion-unica-y-validacion-jwt.md).

---

### ?. Diagrama Final: Arquitectura del spec-09-sesion-unica-y-validacion-jwt

---

### ?. Comandos Git / PR\*\* (una vez validada la implementación y revisados los cambios)

```powershell
git add .
git commit -m "FEPP-ContaOps - spec-09-sesion-unica-y-validacion-jwt -  Implementado"
git push -u origin spec-09-sesion-unica-y-validacion-jwt

# Después de aprobar y fusionar el Pull Request:
git switch main
git pull
git branch -d spec-09-sesion-unica-y-validacion-jwt
```
