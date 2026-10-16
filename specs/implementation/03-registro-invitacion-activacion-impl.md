# Implementar: 03-registro-invitacion-activacion

## PASOS

### 1. Definir UserStatus, resultado de invitación y puertos neutrales.

---

### 2. Implementar adaptador Identity para cuentas pendientes, tokens y activación.

---

### 3. Implementar opciones y validación de invitaciones, URL y key ring.

---

### 4. Definir configuración neutral SMTP y plantillas.

---

### 5. Crear manifest y plantillas HTML/TXT.

---

### 6. Implementar renderer y InvitationEmailNotifier.

---

### 7. Implementar MailKitSmtpEmailSender.

---

### 8. Registrar servicios en el composition root.

---

### 9. Probar transporte SMTP y errores de entrega.

---

### 10. Preparar contratos para SPEC 04.

---

### 11. Actualizar .env.example, README.md y AGENTS.md.

---

#### 6. Validación, ejecución local y Git

**Prerrequisitos:** Docker disponible; crear un archivo `.env` local a partir de `.env.example` y sustituir todos los valores de ejemplo por credenciales y secretos propios. **No subir `.env` a Git.**

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

**Iniciar API:**

```powershell
.\run-local.ps1
```

**Validación del spec (flujo de trabajo existente):**

```powershell
/spec-acceptance-validator @specs/03-registro-invitacion-activacion.md
```

Revisar el spec: [`03-registro-invitacion-activacion.md`](../03-registro-invitacion-activacion.md).

**Comandos Git / PR** (una vez validada la implementación y revisados los cambios):

```powershell
git add .
git commit -m "FEPP-ContaOps - spec-03-registro-invitacion-activacion -  Implementado"
git push -u origin spec-03-registro-invitacion-activacion

# Después de aprobar y fusionar el Pull Request:
git switch main
git pull
git branch -d spec-03-registro-invitacion-activacion
```
