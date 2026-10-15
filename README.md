# FEPP-ContaOps

API de FEPP para procesos operativos contables. La solución utiliza arquitectura
hexagonal y DDD sobre .NET 10, ASP.NET Core Web API, PostgreSQL, Identity, JWT,
FluentValidation y Swagger.

## Requisitos

- .NET SDK 10.
- Docker Desktop con Docker Compose.
- Git.

Verificar la instalación:

```powershell
dotnet --version
docker --version
docker compose version
```

## Estructura

```text
src/
├── Fepp.ContaOps.Domain/          # Modelo y reglas del negocio
├── Fepp.ContaOps.Application/     # Casos de uso y puertos
├── Fepp.ContaOps.Infrastructure/  # EF Core, PostgreSQL, Identity y JWT
└── Fepp.ContaOps.Api/             # HTTP, autenticación y Swagger
```

## 1. Configurar Docker y PostgreSQL

Copiar la plantilla de variables y sustituir los valores localmente. El archivo
`.env` está excluido de Git y no debe publicarse:

```powershell
Copy-Item .env.example .env
notepad .env
```

El archivo debe contener las variables de Docker y de la API. Los nombres con
doble guion bajo (`__`) representan secciones anidadas de la configuración .NET:

```dotenv
# Credencial usada por PostgreSQL dentro del contenedor.
POSTGRES_PASSWORD='replace-with-a-local-database-password'

# Credenciales para ingresar a pgAdmin en http://localhost:5050.
PGADMIN_DEFAULT_EMAIL='jmlahuasiq@gmail.com'
PGADMIN_DEFAULT_PASSWORD='replace-with-a-local-pgadmin-password'

# Cadena de conexión de la API cuando se ejecuta en el host con dotnet run.
# Debe usar Host=localhost y la misma contraseña de POSTGRES_PASSWORD.
ConnectionStrings__Default='Host=localhost;Port=5432;Database=fepp_contaops_dev;Username=app;Password=replace-with-a-local-database-password'

# Configuración del token JWT.
# Jwt__SigningKey debe tener como mínimo 32 bytes y mantenerse secreta.
Jwt__Issuer='FEPP-ContaOps'
Jwt__Audience='FEPP-ContaOps.Api'
Jwt__SigningKey='replace-with-a-random-key-of-at-least-32-bytes'
Jwt__AccessTokenLifetimeMinutes='60'

# Administrador creado automáticamente por el seed.
BootstrapAdmin__Email='jmlahuasiq@gmail.com'
BootstrapAdmin__Password='replace-with-an-admin-password'

# Usuario regular opcional creado automáticamente con el rol User.
BootstrapUser__Email='user@fepp.local'
BootstrapUser__Password='replace-with-a-regular-user-password'

```

Iniciar PostgreSQL y pgAdmin:

```powershell
docker compose -f docker-compose.yml up -d database pgadmin
docker compose -f docker-compose.yml ps
```

Servicios disponibles:

- PostgreSQL desde el host: `localhost:5432`.
- PostgreSQL desde pgAdmin: host `database`, puerto `5432`.
- pgAdmin: `http://localhost:5050`.

Para detener los servicios conservando los datos:

```powershell
docker compose -f docker-compose.yml down
```

Para eliminar también los datos persistidos:

```powershell
docker compose -f docker-compose.yml down --volumes
```

## 2. Cargar configuración desde `.env`

ASP.NET Core no carga archivos `.env` automáticamente. Para ejecutar la API con
`dotnet run`, primero se deben importar sus variables al proceso de PowerShell.
Ejecutar desde la raíz del repositorio:

```powershell
Get-Content .env | ForEach-Object {
    $line = $_.Trim()
    if (-not [string]::IsNullOrWhiteSpace($line) -and -not $line.StartsWith('#')) {
        $parts = $line -split '=', 2
        $value = $parts[1].Trim()
        if (($value.StartsWith("'") -and $value.EndsWith("'")) -or
            ($value.StartsWith('"') -and $value.EndsWith('"'))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        [Environment]::SetEnvironmentVariable($parts[0].Trim(), $value, 'Process')
    }
}

dotnet run --project src/Fepp.ContaOps.Api
```

Las variables se mantienen únicamente en la sesión actual de PowerShell. Para
confirmar que una variable fue cargada sin mostrar su valor completo:

```powershell
if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__Default)) {
    throw 'ConnectionStrings__Default no fue cargada.'
}
```

También es posible usar User Secrets en desarrollo, pero no es necesario si se
usa este procedimiento con `.env`.

## 3. Seed de Identity

El seed se ejecuta automáticamente durante el arranque de la API. No se ejecuta
con un comando separado:

1. Crea el esquema de Identity si todavía no existe.
2. Crea los roles `Admin` y `User`.
3. Crea el administrador inicial si no existe.
4. No sobrescribe la contraseña de un usuario existente.
5. Crea opcionalmente un usuario regular con rol `User`.

El administrador y el usuario regular se configuran en `.env` mediante
`BootstrapAdmin__*` y `BootstrapUser__*`. No guardar contraseñas reales en este
README ni en archivos versionados.

La contraseña debe tener al menos 12 caracteres, mayúscula, minúscula, dígito,
símbolo y seis caracteres únicos.

## 4. Compilar y levantar la API

Ejecutar desde la raíz del repositorio:

```powershell
dotnet restore FEPP-ContaOps.slnx
dotnet build FEPP-ContaOps.slnx
```

Después de compilar, cargar `.env` como se indicó en la sección 2 y ejecutar:

```powershell
dotnet run --project src/Fepp.ContaOps.Api
```

Al iniciar, la aplicación ejecuta automáticamente el seed. Si falta
`ConnectionStrings:Default`, JWT o `BootstrapAdmin`, el arranque falla de forma
intencional para evitar una configuración insegura.

## 5. Rutas y acceso manual

Con el perfil HTTPS de `launchSettings.json`:

- API base: `https://localhost:7077`.
- API HTTP: `http://localhost:5282`.
- Swagger UI: `https://localhost:7077/swagger`.
- Documento OpenAPI: `https://localhost:7077/swagger/v1/swagger.json`.
- pgAdmin: `http://localhost:5050`.

### Login

`POST /api/auth/login`

Request:

```json
{
  "email": "jmlahuasiq@gmail.com",
  "password": "LA_CLAVE_ADMINISTRATIVA_CONFIGURADA"
}
```

Respuesta exitosa:

```json
{
  "accessToken": "...",
  "tokenType": "Bearer",
  "expiresIn": 3600
}
```

Credenciales inválidas devuelven `401` con `code` y `traceId`.

### Swagger con Bearer

1. Abrir `/swagger`.
2. Ejecutar `POST /api/auth/login` y copiar `accessToken`.
3. Pulsar **Authorize**.
4. Introducir `Bearer <accessToken>`.
5. Pulsar **Authorize** y cerrar la ventana.

El esquema Bearer ya está registrado en Swagger. Las futuras rutas protegidas
usarán el token mediante `UseAuthentication` y `UseAuthorization`.

## 6. pgAdmin

pgAdmin se ejecuta en un contenedor independiente. La conexión a PostgreSQL
debe realizarse por la red interna de Docker Compose, usando el nombre del
servicio `database`, no `localhost`.

### Ingresar a pgAdmin

1. Abrir `http://localhost:5050`.
2. Ingresar con `PGADMIN_DEFAULT_EMAIL` del `.env`.
3. Ingresar con `PGADMIN_DEFAULT_PASSWORD` del `.env`.

### Registrar el servidor PostgreSQL

Si el panel izquierdo muestra `Servers` vacío, es necesario registrar la
conexión manualmente:

1. En el panel izquierdo, hacer clic derecho sobre **Servers**.
2. Seleccionar **Register > Server**.
3. En la pestaña **General**, completar:

   ```text
   Name: FEPP local
   ```

4. Abrir la pestaña **Connection** y completar:

   ```text
   Host name/address: database
   Port: 5432
   Maintenance database: fepp_contaops_dev
   Username: app
   Password: valor de POSTGRES_PASSWORD del archivo .env
   Save password: activado
   ```

5. Pulsar **Save**.

El campo `FEPP local` es únicamente el nombre visible de la conexión. No debe
escribirse en `Host name/address`; el host correcto es `database`.

La conexión utiliza esta ruta interna:

```text
pgAdmin (contenedor) ──► database:5432 ──► PostgreSQL (contenedor)
```

Después de guardar, expandir:

```text
Servers
└── FEPP local
    └── Databases
        └── fepp_contaops_dev
            └── Schemas
                └── public
                    └── Tables
```

Las tablas de Identity creadas por el seed son `AspNetUsers`, `AspNetRoles`,
`AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins` y
`AspNetUserTokens`.

Si pgAdmin no conecta, verificar el estado de los contenedores:

```powershell
docker compose -f docker-compose.yml ps
docker compose -f docker-compose.yml logs --tail=50 pgadmin
```

PostgreSQL debe aparecer como `healthy`. No usar `docker compose down --volumes`
para solucionar problemas de conexión salvo que se quiera eliminar toda la
base de datos y sus usuarios.

## 7. Seguridad del archivo `.env`

El archivo `.env` contiene contraseñas y la clave de firma JWT. Debe permanecer
fuera de Git, tener permisos restringidos y no incluirse en imágenes Docker ni
en respaldos sin cifrar. Para producción se recomienda inyectar estas mismas
variables mediante el sistema de despliegue o un gestor de secretos.

La clave `Jwt__SigningKey` debe tener al menos 32 bytes. No reutilizar la misma
contraseña para PostgreSQL, el administrador y el usuario regular.

## 8. Pruebas automatizadas

Las pruebas unitarias no requieren Docker. Las de integración utilizan un
contenedor PostgreSQL efímero y requieren Docker activo:

```powershell
dotnet test FEPP-ContaOps.slnx
```

## 9. Logs

La API escribe logs estructurados en formato JSON en:

```text
src/Fepp.ContaOps.Api/logs/fepp-contaops-YYYYMMDD.json
```

La consola muestra únicamente el estado de arranque, las URLs de la API,
Swagger, OpenAPI y las rutas principales. Las solicitudes exitosas no se
imprimen en consola ni se guardan como eventos de información; las advertencias,
errores y excepciones sí se conservan en los archivos de logs.

La rotación se realiza:

- diariamente;
- al alcanzar 10 MB por archivo;
- conservando como máximo 14 archivos.

El directorio `logs/` está excluido de Git. Los logs no deben contener
contraseñas, tokens JWT ni información sensible.

## Documentación complementaria

- [Creación e instalación](sdd/create-install.md)
- [Arquitectura de la Web API](sdd/webapi-architecture.md)
- [Modelo e importación del balance global](sdd/global-balance-sheet.md)
- [Naming y documentación de código](sdd/dotnet-naming-and-code-documentation.md)
