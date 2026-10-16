# FEPP-ContaOps

API contable de FEPP basada en .NET 10, ASP.NET Core, PostgreSQL, Identity,
JWT, FluentValidation y Swagger, organizada con arquitectura hexagonal y DDD.

## Inicio rápido

| Paso | Acción | Comando |
|---:|---|---|
| 1 | Crear configuración | `Copy-Item .env.example .env` |
| 2 | Editar valores locales | `notepad .env` |
| 3 | Iniciar PostgreSQL y pgAdmin | `docker compose up -d database pgadmin` |
| 4 | Confiar en HTTPS, una vez | `dotnet dev-certs https --trust` |
| 5 | Ejecutar la API | `.\run-local.ps1` |

`run-local.ps1` carga `.env` y ejecuta la API con el perfil HTTPS. No inicia ni
detiene contenedores Docker.

## Requisitos

| Herramienta | Uso | Verificación |
|---|---|---|
| .NET SDK 10 | API y pruebas | `dotnet --version` |
| Docker Desktop | PostgreSQL y pruebas de integración | `docker --version` |
| Docker Compose | Servicios locales | `docker compose version` |
| Git | Control de versiones | `git --version` |

## Estructura

| Proyecto | Responsabilidad |
|---|---|
| `Fepp.ContaOps.Domain` | Modelo y reglas de negocio |
| `Fepp.ContaOps.Application` | Casos de uso y puertos |
| `Fepp.ContaOps.Infrastructure` | Persistencia, Identity, JWT y correo |
| `Fepp.ContaOps.Api` | HTTP, autenticación y Swagger |

## Configuración

`.env` está excluido de Git y no debe publicarse. ASP.NET Core no carga `.env`
automáticamente; `run-local.ps1` lo importa para la ejecución local.

| Grupo | Variables | Secretos |
|---|---|---:|
| PostgreSQL | `POSTGRES_PASSWORD`, `ConnectionStrings__Default` | Sí |
| pgAdmin | `PGADMIN_DEFAULT_EMAIL`, `PGADMIN_DEFAULT_PASSWORD` | Contraseña |
| JWT | `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey`, `Jwt__AccessTokenLifetimeMinutes` | `SigningKey` |
| Administrador | `BootstrapAdmin__Email`, `BootstrapAdmin__Password` | Contraseña |
| Invitaciones | `UserInvitation__TokenLifetimeHours`, `UserInvitation__ActivationBaseUrl` | No |
| SMTP | `Email__Smtp__*` | `Password` |
| Plantillas | `Email__Templates__RootPath` | No |
| Data Protection | `DataProtection__KeyRingPath` | No |

Reglas principales:

| Elemento | Regla |
|---|---|
| Variables anidadas | Usar doble guion bajo (`__`) |
| PostgreSQL desde la API | `Host=localhost` |
| PostgreSQL desde pgAdmin | Host `database` |
| SMTP Gmail | `smtp.gmail.com:587` con `StartTls` |
| Autenticación Gmail | Contraseña de aplicación; no API key |
| URL de activación | HTTPS obligatorio |
| Usuarios regulares | Se crean mediante invitación; no existe `BootstrapUser` |
| Valores reales | Solo en `.env`, User Secrets o variables seguras |

El listado completo de placeholders debe mantenerse en `.env.example`.

## Servicios y direcciones

| Servicio | Desde el host | Desde Docker |
|---|---|---|
| PostgreSQL | `localhost:5432` | `database:5432` |
| pgAdmin | `http://localhost:5050` | — |
| API HTTPS | `https://localhost:7077` | — |
| API HTTP | `http://localhost:5282` | — |
| Swagger | `https://localhost:7077/swagger` | — |
| OpenAPI | `https://localhost:7077/swagger/v1/swagger.json` | — |

## Ejecutar la API

### Opción recomendada

```powershell
.\run-local.ps1
```

| El script | Comportamiento |
|---|---|
| `.env` | Verifica, valida y carga variables en el proceso |
| Proyecto | Verifica el proyecto API y `dotnet` |
| Perfil | Ejecuta el perfil HTTPS |
| Docker | No inicia, detiene ni elimina contenedores |
| `Ctrl+C` | Detiene la API y conserva los contenedores |

### Opción manual

```powershell
dotnet restore FEPP-ContaOps.slnx
dotnet build FEPP-ContaOps.slnx
dotnet run --project src/Fepp.ContaOps.Api --launch-profile https
```

## Identity y endpoints

El seed se ejecuta al iniciar la API:

| Operación | Resultado |
|---|---|
| Esquema | Se crea si no existe |
| Roles | Crea `Admin` y `User` |
| Administrador | Se crea si no existe |
| Contraseña existente | No se reemplaza |
| Usuario regular | Se crea mediante invitación |
| Configuración inválida | Impide el arranque |

| Método | Ruta | Acceso | Uso |
|---|---|---|---|
| `POST` | `/api/auth/login` | Anónimo | Obtener JWT |
| `POST` | `/api/users/invitations` | `Admin` | Crear invitación |
| `POST` | `/api/auth/activate` | Anónimo | Activar cuenta |
| `POST` | `/api/users/{id}/invitations` | `Admin` | Reenviar invitación |

### Login

```json
{
  "email": "admin@example.local",
  "password": "LA_CLAVE_ADMINISTRATIVA_CONFIGURADA"
}
```

Las credenciales inválidas devuelven `401` con `code` y `traceId`. Para usar
Swagger, abrir `/swagger`, ejecutar login, pulsar **Authorize** e introducir
`Bearer <accessToken>`.

## pgAdmin

Abrir `http://localhost:5050` e iniciar sesión con las variables del `.env`.
Registrar PostgreSQL con estos valores:

| Campo | Valor |
|---|---|
| Name | `FEPP local` |
| Host | `database` |
| Port | `5432` |
| Database | `fepp_contaops_dev` |
| Username | `app` |
| Password | Valor de `POSTGRES_PASSWORD` |

Diagnóstico:

```powershell
docker compose ps
docker compose logs --tail=50 pgadmin
```

PostgreSQL debe aparecer como `healthy`. Para detener servicios conservando
datos usar `docker compose down`. `docker compose down --volumes` elimina los
datos locales.

## Comandos operativos

| Acción | Comando |
|---|---|
| Restaurar | `dotnet restore FEPP-ContaOps.slnx` |
| Compilar | `dotnet build FEPP-ContaOps.slnx` |
| Ejecutar API | `.\run-local.ps1` |
| Probar | `dotnet test FEPP-ContaOps.slnx` |
| Ver contenedores | `docker compose ps` |
| Detener Docker | `docker compose down` |

Las pruebas de integración requieren Docker activo y no conectan con Gmail real.

## Seguridad y logs

| Regla | Aplicación |
|---|---|
| `.env` | No versionar ni publicar |
| `.env.example` | Solo placeholders |
| JWT y SMTP | Secretos fuera de Git |
| Contraseñas | No reutilizarlas entre servicios |
| Logs | No contienen contraseñas, tokens ni secretos |
| Key ring Docker | Volumen persistente y protegido |
| Logs | `src/Fepp.ContaOps.Api/logs/`, rotación diaria y máximo 14 archivos |

## Documentación complementaria

| Documento | Contenido |
|---|---|
| [Creación e instalación](sdd/create-install.md) | Solución, paquetes, Docker y migraciones |
| [Arquitectura Web API](sdd/webapi-architecture.md) | Capas y dependencias |
| [Balance global](sdd/global-balance-sheet.md) | Modelo e importación contable |
| [Naming y documentación](sdd/dotnet-naming-and-code-documentation.md) | Convenciones .NET |
