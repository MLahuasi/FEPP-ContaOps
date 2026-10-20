# Implementar: 08-persistencia-sesiones-refresh-tokens

## PASOS

### 1. Recrear PostgreSQL y pgAdmin desde cero.

---

### 2. Iniciar database y pgadmin, verificando su estado.

---

### 3. Registrar pgAdmin.

#### Recuperación manual de `ERR_EMPTY_RESPONSE`

En el primer arranque, pgAdmin puede tardar en completar la inicialización de
su base interna. Durante ese intervalo el navegador puede mostrar:
`localhost no ha enviado ningún dato (ERR_EMPTY_RESPONSE)`.

El usuario debe seguir este procedimiento:

1. No eliminar volúmenes ni ejecutar `docker compose down --volumes`, porque
   eso borra la configuración local de pgAdmin y obliga a empezar de nuevo.
2. Abrir PowerShell en la raíz del repositorio y comprobar el estado:

   ```powershell
   docker compose ps
   docker compose logs --tail=100 pgadmin
   ```

3. Esperar hasta que los logs indiquen que Gunicorn está escuchando en
   `http://[::]:80` y que el contenedor aparezca como `Up`.
4. Recargar `http://localhost:5050/` (o abrirlo de nuevo en una ventana
   privada). Si continúa sin responder después de finalizar la inicialización,
   reiniciar únicamente pgAdmin:

   ```powershell
   docker compose restart pgadmin
   ```

5. Esperar nuevamente a que termine la inicialización y volver a abrir la
   URL. Si el problema persiste, consultar los últimos logs:

   ```powershell
   docker compose logs --tail=200 pgadmin
   ```

Cuando la pantalla de login esté disponible, iniciar sesión con
`PGADMIN_DEFAULT_EMAIL` y `PGADMIN_DEFAULT_PASSWORD`; después registrar
`FEPP-ContaOps Local` con host `database`, puerto `5432`, base
`fepp_contaops_dev`, usuario `app` y la contraseña local de
`POSTGRES_PASSWORD`.

---

### 4. Crear entidades en Infrastructure.

---

### 5. Configurar relaciones, índices, unicidad y cascada.

---

### 6. Implementar generación, hash y comparación.

---

### 7. Implementar sesión activa y refresh activo.

---

### 8. Implementar consumo, revocación y expiración.

---

### 9. Agregar pruebas unitarias.

---

### 10. Iniciar API, verificar tablas y ejecutar build/pruebas.

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
/spec-acceptance-validator @specs/08-persistencia-sesiones-refresh-tokens.md
```

Revisar el spec: [`08-persistencia-sesiones-refresh-tokens.md`](../08-persistencia-sesiones-refresh-tokens.md).

---

### ?. Diagrama Final: Arquitectura del spec-08-persistencia-sesiones-refresh-tokens

---

### ?. Comandos Git / PR\*\* (una vez validada la implementación y revisados los cambios)

```powershell
git add .
git commit -m "FEPP-ContaOps - spec-08-persistencia-sesiones-refresh-tokens -  Implementado"
git push -u origin spec-08-persistencia-sesiones-refresh-tokens

# Después de aprobar y fusionar el Pull Request:
git switch main
git pull
git branch -d spec-08-persistencia-sesiones-refresh-tokens
```
