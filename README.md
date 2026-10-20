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

Para una ejecución manual sin `run-local.ps1`, importa las variables al proceso
antes de ejecutar la API. PowerShell no carga `.env` automáticamente; puedes
usar el script recomendado o asignar las variables con
`$env:Nombre='valor'`. No pongas secretos en archivos versionados.

| Grupo | Variables | Secretos |
|---|---|---:|
| PostgreSQL | `POSTGRES_PASSWORD`, `ConnectionStrings__Default` | Sí |
| pgAdmin | `PGADMIN_DEFAULT_EMAIL`, `PGADMIN_DEFAULT_PASSWORD` | Contraseña |
| JWT | `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey`, `Jwt__AccessTokenLifetimeMinutes` | `SigningKey` |
| Administrador | `BootstrapAdmin__Email`, `BootstrapAdmin__Password` | Contraseña |
| Contraseña Identity | `Identity__Password__*` | No |
| Bloqueo Identity | `Identity__Lockout__*` | No |
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
| SMTP seguro | Solo `StartTls` en 587 o `TlsOnConnect` en 465 |
| Usuarios regulares | Se crean exclusivamente mediante invitación |
| Valores reales | Solo en `.env`, User Secrets o variables seguras |

El listado completo de placeholders debe mantenerse en `.env.example`.

### SMTP y Gmail

Gmail es el proveedor inicial mediante SMTP compatible. Activa la verificación
en dos pasos y crea una **contraseña de aplicación** de Google; una API key de
Google no autentica SMTP. Configura la cuenta en
`Email__Smtp__Username` y `Email__Smtp__FromAddress`, y la contraseña de
aplicación únicamente en `Email__Smtp__Password`.

El contrato no depende de Gmail: otro proveedor compatible puede sustituirse
cambiando `Email__Smtp__*`. No se usan `MAILER_*`, `MAIL_*` ni
`MAIL_TEMPLATE_EXTENSION`.

### Plantillas y key ring

El catálogo base está en `src/Fepp.ContaOps.Infrastructure/Email/Templates/` y
contiene `templates.json`, `invitation.html` e `invitation.txt`. Se puede
reemplazar mediante `Email__Templates__RootPath`; el manifest y ambos formatos
se validan al iniciar.

`DataProtection__KeyRingPath` es opcional para desarrollo local. En Windows
local, el key ring puede protegerse mediante DPAPI. La API actualmente se
ejecuta en el host con `run-local.ps1`; por ello el `docker-compose.yml` actual
no monta todavía un volumen de key ring para la API.

Cuando la API se ejecute en Docker, `DataProtection__KeyRingPath` deberá apuntar
a un directorio montado desde un named volume con permisos de lectura y
escritura para la API. El volumen deberá conservarse al recrear el contenedor;
el almacenamiento anfitrión deberá contar con protección en reposo.

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

### Pruebas automatizadas

```powershell
# Pruebas unitarias; no requieren Docker.
dotnet test tests/Fepp.ContaOps.UnitTests/Fepp.ContaOps.UnitTests.csproj

# Pruebas de integración; requieren Docker activo.
dotnet test tests/Fepp.ContaOps.IntegrationTests/Fepp.ContaOps.IntegrationTests.csproj

# Suite completa.
dotnet test FEPP-ContaOps.slnx
```

Las pruebas de integración utilizan PostgreSQL efímero mediante Testcontainers,
un notificador de invitaciones falso y autenticación aislada para comprobar los
roles `Admin` y `User`. No utilizan Gmail ni credenciales SMTP reales.

Si el build informa que `Fepp.ContaOps.Api.exe` está bloqueado, detén primero
la API con `Ctrl+C` y vuelve a ejecutar el comando.

### Pruebas manuales desde Swagger

Para probar la API manualmente:

1. Copia `.env.example` a `.env` y edita `.env`; `.env.example` nunca se carga.
2. Inicia PostgreSQL y pgAdmin con Docker.
3. Ejecuta `.\run-local.ps1` desde la raíz del repositorio.
4. Abre exclusivamente `https://localhost:7077/swagger`.
5. Ejecuta `POST /api/auth/login` con el administrador configurado.
6. Copia únicamente `accessToken` y pulsa **Authorize**. Swagger añade
   automáticamente el prefijo `Bearer`.

No utilices `http://localhost:5282/swagger` para esta prueba. Si el navegador
indica `Failed to fetch`, acepta el certificado HTTPS de desarrollo y verifica
que la API se esté ejecutando con el perfil `https`.

El seed crea el administrador solo si no existe y no reemplaza su contraseña.
Cambiar `BootstrapAdmin__Password` después del primer arranque no cambia la
contraseña almacenada; para una base local descartable se puede recrear el
volumen con `docker compose down --volumes`.

## Identity y endpoints

El seed se ejecuta al iniciar la API:

| Operación | Resultado |
|---|---|
| Esquema | Se crea si no existe |
| Roles | Crea `Admin` y `User` |
| Administrador | Se crea si no existe |
| Contraseña existente | No se reemplaza |
| Usuario regular | Se crea mediante invitación |
| Política de contraseña | Se valida y configura mediante `Identity__Password__*` |
| Bloqueo | Se configura mediante `Identity__Lockout__*` |
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

### Invitaciones y activación

Un administrador autenticado crea una cuenta pendiente:

```http
POST /api/users/invitations
Authorization: Bearer <admin-jwt>
Content-Type: application/json
```

```json
{
  "email": "user@example.com"
}
```

La respuesta `201 Created` contiene únicamente `id`, `email` y `role`. El token
y el enlace se entregan exclusivamente mediante el correo de invitación.

El cliente activa la cuenta de forma anónima con el token recibido:

```http
POST /api/auth/activate
Content-Type: application/json
```

```json
{
  "userId": "<user-id>",
  "token": "<opaque-token>",
  "newPassword": "<password>"
}
```

Una activación correcta devuelve `204 No Content`. La contraseña debe cumplir
la política efectiva de Identity; la API solo valida los límites de transporte.

La cuenta creada por invitación recibe el rol `User`. Puede iniciar sesión,
pero no puede crear ni reenviar invitaciones; esas operaciones requieren el rol
`Admin` y devuelven `403 forbidden` para un usuario regular.

#### Activación sin frontend

`UserInvitation__ActivationBaseUrl` debe apuntar a una ruta HTTPS real del
frontend, por ejemplo:

```env
UserInvitation__ActivationBaseUrl='https://frontend.local/activate'
```

La API no implementa una página `GET /activate`; solo expone
`POST /api/auth/activate`. Por eso, abrir un enlace configurado como
`https://localhost:7077/activate` devuelve `404` si no existe un frontend en
esa ruta.

Mientras no exista frontend, se puede activar desde Swagger con:

```json
{
  "userId": "<user-id>",
  "token": "<decoded-token>",
  "newPassword": "<password>"
}
```

El token incluido en el fragmento de la URL debe decodificarse antes de enviarlo
(`%2F` es `/`, `%2B` es `+`). Por ejemplo, en el navegador:

```javascript
decodeURIComponent("TOKEN_COPIADO_DE_LA_URL")
```

Solo funciona el token más reciente: reenviar una invitación invalida el token
anterior. Una activación correcta devuelve `204 No Content`.

Para una cuenta que todavía está pendiente, un administrador puede invalidar la
invitación anterior y enviar una nueva:

```http
POST /api/users/{id}/invitations
Authorization: Bearer <admin-jwt>
```

El reenvío correcto devuelve `204 No Content`. No se puede reenviar una cuenta
activa.

### Errores de API

Los errores usan `application/problem+json` e incluyen `code` y `traceId`.

| Situación | HTTP | Código |
|---|---:|---|
| Request inválido | 400 | `validation_error` |
| Token inválido, vencido o usado | 400 | `invalid_activation_token` |
| Sin JWT | 401 | `authentication_required` |
| JWT sin rol `Admin` | 403 | `forbidden` |
| Usuario inexistente para reenvío | 404 | `resource_not_found` |
| Email duplicado o cuenta activa | 409 | `resource_conflict` |
| Correo no disponible | 503 | `email_delivery_unavailable` |

Las respuestas no exponen contraseñas, tokens, enlaces completos, security
stamps ni credenciales SMTP.

## pgAdmin

Abrir `http://localhost:5050` e iniciar sesión con las variables del `.env`.
El login web de pgAdmin utiliza `PGADMIN_DEFAULT_EMAIL` y
`PGADMIN_DEFAULT_PASSWORD`; el usuario PostgreSQL `app` se utiliza después,
al registrar el servidor, y no sirve para entrar a la pantalla de pgAdmin.
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
Las pruebas SMTP usan servidores en proceso y nunca credenciales ni servidores
Gmail reales.

## Diagnóstico rápido

| Síntoma | Causa probable | Acción |
|---|---|---|
| `Failed to fetch` en Swagger | HTTP, certificado HTTPS o API detenida | Abrir `https://localhost:7077/swagger` y confiar en el certificado |
| `401` con `/Account/Login` | Esquema de cookies en vez de JWT | Reiniciar la API con la versión actual y usar el JWT del administrador |
| `401 authentication_required` | JWT ausente, inválido o vencido | Volver a ejecutar login y autorizar Swagger |
| `403 forbidden` | JWT válido con rol `User` | Usar el token del administrador |
| `404` en `/activate` | No existe frontend de activación | Activar mediante `POST /api/auth/activate` |
| `invalid_activation_token` | Token codificado, usado, vencido o anterior | Decodificarlo o reenviar la invitación |
| El enlace sigue usando `cliente.local` | `.env` incorrecto, API antigua o enlace anterior | Reiniciar la API y generar una invitación nueva |
| Fallo de build por `.exe` bloqueado | La API sigue ejecutándose | Detenerla con `Ctrl+C` |

Para verificar la configuración cargada por el script sin mostrar secretos:

```powershell
Select-String -Path .env -Pattern '^UserInvitation__'
```

Los tokens, contraseñas y enlaces completos no deben copiarse en capturas ni
logs. Si un token fue expuesto, reenvía la invitación para invalidarlo.

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
| [API de invitaciones](sdd/api-invitations.md) | Endpoints, autorización y errores |
