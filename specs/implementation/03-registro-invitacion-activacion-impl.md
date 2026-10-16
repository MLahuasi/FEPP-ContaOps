# Implementar: 03-registro-invitacion-activacion

## PASOS

### 1. Definir UserStatus, resultado de invitación y puertos neutrales

#### Componentes implementados

| Componente               | Acción | Tipo        | Capa        | Responsabilidad                                                           |
| ------------------------ | ------ | ----------- | ----------- | ------------------------------------------------------------------------- |
| `UserStatus`             | Create | `enum`      | Domain      | Define los estados `PendingActivation` y `Active`.                        |
| `User`                   | Update | `class`     | Domain      | Incorpora la propiedad `Status` para identificar el estado de la cuenta.  |
| `UserInvitationResult`   | Create | `record`    | Application | Contiene el usuario y su token de activación.                             |
| `IUserInvitationService` | Create | `interface` | Application | Define las operaciones de registro, activación y reenvío de invitaciones. |
| `IInvitationNotifier`    | Create | `interface` | Application | Define el contrato para notificar invitaciones.                           |
| `IEmailSender`           | Create | `interface` | Application | Define el contrato para enviar correos electrónicos.                      |
| `EmailMessage`           | Create | `record`    | Application | Representa un mensaje de correo independiente del proveedor.              |

**Arquitectura:** los contratos se definen en Application y sus implementaciones técnicas podrán desarrollarse en Infrastructure, siguiendo Clean Architecture.

#### Estados del usuario

| Estado              | Descripción                                                                    |
| ------------------- | ------------------------------------------------------------------------------ |
| `PendingActivation` | Cuenta pendiente de activación.                                                |
| `Active`            | Cuenta activada, habilitada para autenticarse si cumple las demás condiciones. |

#### Métodos y funciones principales

| Clase / Interfaz         | Método                   | Responsabilidad                                              |
| ------------------------ | ------------------------ | ------------------------------------------------------------ |
| `User`                   | `User()`                 | Construye el usuario y valida identificador, correo y roles. |
| `IUserInvitationService` | `RegisterPendingAsync()` | Registra una cuenta pendiente y genera una invitación.       |
| `IUserInvitationService` | `ActivateAsync()`        | Activa una cuenta estableciendo su contraseña inicial.       |
| `IUserInvitationService` | `ResendAsync()`          | Genera una nueva invitación.                                 |
| `IInvitationNotifier`    | `NotifyAsync()`          | Solicita la notificación de una invitación.                  |
| `IEmailSender`           | `SendAsync()`            | Solicita el envío de un correo electrónico.                  |

#### Modelos de datos

| Modelo                 | Propiedad         | Tipo     | Descripción                         |
| ---------------------- | ----------------- | -------- | ----------------------------------- |
| `UserInvitationResult` | `User`            | `User`   | Usuario asociado a la invitación.   |
| `UserInvitationResult` | `ActivationToken` | `string` | Token interno de activación.        |
| `EmailMessage`         | `To`              | `string` | Destinatario.                       |
| `EmailMessage`         | `Subject`         | `string` | Asunto.                             |
| `EmailMessage`         | `HtmlBody`        | `string` | Contenido HTML.                     |
| `EmailMessage`         | `TextBody`        | `string` | Contenido en texto plano.           |
| `EmailMessage`         | `AttemptId`       | `string` | Identificador del intento de envío. |

#### Componentes de .NET utilizados

| Componente                             | Origen | Función                                            |
| -------------------------------------- | ------ | -------------------------------------------------- |
| `Task<T>`                              | .NET   | Representa operaciones asíncronas.                 |
| `CancellationToken`                    | .NET   | Permite solicitar la cancelación de operaciones.   |
| `IReadOnlyCollection<T>`               | .NET   | Expone colecciones sin métodos de modificación.    |
| `IEnumerable<T>`                       | .NET   | Representa una secuencia de elementos.             |
| `ArgumentNullException.ThrowIfNull()`  | .NET   | Valida referencias nulas.                          |
| `Where()` / `Distinct()` / `ToArray()` | LINQ   | Filtran y procesan colecciones.                    |
| `StringComparer.OrdinalIgnoreCase`     | .NET   | Compara cadenas ignorando mayúsculas y minúsculas. |

`Result<T>` y `UserId` son componentes propios del proyecto.

#### Diagrama de arquitectura

```mermaid
flowchart TD
    subgraph Domain
        U["User"]
        S["UserStatus"]
        U --> S
    end

    subgraph Application
        UIS["IUserInvitationService"]
        UIR["UserInvitationResult"]
        IN["IInvitationNotifier"]
        ES["IEmailSender"]
        EM["EmailMessage"]

        UIS --> UIR
        IN --> UIR
        ES --> EM
    end

    subgraph Infrastructure
        IMP["Implementaciones de los puertos"]
    end

    UIR --> U
    IMP -.->|Implementa| UIS
    IMP -.->|Implementa| IN
    IMP -.->|Implementa| ES
```

**Interpretación:**

- **Domain:** define el usuario y sus estados funcionales.
- **Application:** establece los contratos para registrar, activar y notificar usuarios.
- **Infrastructure:** implementará los contratos utilizando servicios de identidad, persistencia y correo.

#### Las implementaciones de Infrastructure se muestran como componentes previstos; no forman parte de los cambios de este paso.

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
