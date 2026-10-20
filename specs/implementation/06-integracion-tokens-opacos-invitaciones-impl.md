# Implementar: 06-integracion-tokens-opacos-invitaciones

## PASOS

### 1. Verificar PostgreSQL y pgAdmin con docker compose ps, sin eliminar volúmenes.

---

### 2. Implementar HashedUserTokenProvider.

---

### 3. Registrar únicamente el proveedor dedicado en Identity.

---

### 4. Adaptar la creación de invitaciones para usar el nuevo token.

---

### 5. Adaptar el reenvío, rotando SecurityStamp y reemplazando el token anterior.

---

### 6. Adaptar la activación para validar y consumir el token atómicamente.

---

### 7. Invalidar tokens antiguos de Data Protection.

---

### 8. Agregar pruebas de tokens válidos, inválidos, vencidos, usados, de otro usuario y de reenvío.

---

### 9. Agregar prueba de dos activaciones concurrentes, asegurando que solo una tenga éxito.

---

### 10. Ejecutar build, pruebas unitarias, integración con Testcontainers y revisar que ningún token aparezca en respuestas o logs.

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
/spec-acceptance-validator @specs/06-integracion-tokens-opacos-invitaciones.md
```

Revisar el spec: [`06-integracion-tokens-opacos-invitaciones.md`](../06-integracion-tokens-opacos-invitaciones.md).

---

### 14. Diagrama Final: Arquitectura del spec-06-integracion-tokens-opacos-invitaciones

---

### 15. Comandos Git / PR\*\* (una vez validada la implementación y revisados los cambios)

```powershell
git add .
git commit -m "FEPP-ContaOps - spec-06-integracion-tokens-opacos-invitaciones -  Implementado"
git push -u origin spec-06-integracion-tokens-opacos-invitaciones

# Después de aprobar y fusionar el Pull Request:
git switch main
git pull
git branch -d spec-06-integracion-tokens-opacos-invitaciones
```
