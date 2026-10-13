# Creación e instalación de FEPP-ContaOps

Esta guía contiene los comandos para crear la solución, instalar dependencias, configurar servicios locales y arrancar la API.

## Requisitos

- SDK .NET 10.
- Git.
- Docker Desktop o Docker Engine con Docker Compose.
- Acceso al directorio de archivos de la Fundación FEPP que contiene `Plan_Cuentas`.

Verificar herramientas:

```powershell
dotnet --version
git --version
docker --version
docker compose version
```

## Crear solución y proyectos

Ejecutar los comandos desde el directorio raíz del repositorio:

```powershell
dotnet new sln --name FEPP-ContaOps
dotnet new classlib --name Fepp.ContaOps.Domain --output src/Fepp.ContaOps.Domain
dotnet new classlib --name Fepp.ContaOps.Application --output src/Fepp.ContaOps.Application
dotnet new classlib --name Fepp.ContaOps.Infrastructure --output src/Fepp.ContaOps.Infrastructure
dotnet new webapi --name Fepp.ContaOps.Api --output src/Fepp.ContaOps.Api --use-controllers

dotnet sln FEPP-ContaOps.slnx add src/Fepp.ContaOps.Domain/Fepp.ContaOps.Domain.csproj
dotnet sln FEPP-ContaOps.slnx add src/Fepp.ContaOps.Application/Fepp.ContaOps.Application.csproj
dotnet sln FEPP-ContaOps.slnx add src/Fepp.ContaOps.Infrastructure/Fepp.ContaOps.Infrastructure.csproj
dotnet sln FEPP-ContaOps.slnx add src/Fepp.ContaOps.Api/Fepp.ContaOps.Api.csproj

dotnet add src/Fepp.ContaOps.Application/Fepp.ContaOps.Application.csproj reference src/Fepp.ContaOps.Domain/Fepp.ContaOps.Domain.csproj
dotnet add src/Fepp.ContaOps.Infrastructure/Fepp.ContaOps.Infrastructure.csproj reference src/Fepp.ContaOps.Application/Fepp.ContaOps.Application.csproj
dotnet add src/Fepp.ContaOps.Infrastructure/Fepp.ContaOps.Infrastructure.csproj reference src/Fepp.ContaOps.Domain/Fepp.ContaOps.Domain.csproj
dotnet add src/Fepp.ContaOps.Api/Fepp.ContaOps.Api.csproj reference src/Fepp.ContaOps.Application/Fepp.ContaOps.Application.csproj
dotnet add src/Fepp.ContaOps.Api/Fepp.ContaOps.Api.csproj reference src/Fepp.ContaOps.Infrastructure/Fepp.ContaOps.Infrastructure.csproj
```

Con el SDK .NET 10, `dotnet new sln` crea `FEPP-ContaOps.slnx` por defecto.

## Dependencias NuGet

### Application

`ILogger<T>` permite registrar eventos de casos de uso sin acoplar Application a un proveedor concreto:

```powershell
dotnet add src/Fepp.ContaOps.Application package Microsoft.Extensions.Logging.Abstractions --version 10.0.12
```

### Infrastructure

```powershell
dotnet add src/Fepp.ContaOps.Infrastructure package Microsoft.EntityFrameworkCore --version 10.0.12
dotnet add src/Fepp.ContaOps.Infrastructure package Microsoft.EntityFrameworkCore.Design --version 10.0.12
dotnet add src/Fepp.ContaOps.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3
dotnet add src/Fepp.ContaOps.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.12
dotnet add src/Fepp.ContaOps.Infrastructure package System.IdentityModel.Tokens.Jwt --version 8.23.0
dotnet add src/Fepp.ContaOps.Infrastructure package PdfPig --version 0.1.16
dotnet add src/Fepp.ContaOps.Infrastructure package ClosedXML --version 0.105.1
```

### Api

```powershell
dotnet add src/Fepp.ContaOps.Api package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.12
dotnet add src/Fepp.ContaOps.Api package FluentValidation.DependencyInjectionExtensions --version 12.2.0
dotnet add src/Fepp.ContaOps.Api package Swashbuckle.AspNetCore --version 10.3.0
dotnet add src/Fepp.ContaOps.Api package Microsoft.Extensions.Compliance.Redaction --version 10.10.0
```

`Microsoft.Extensions.Compliance.Redaction` permite clasificar y redactar valores que se incorporen explícitamente al flujo de logging. No sustituye la regla de no registrar información sensible. Domain no requiere paquetes NuGet inicialmente. Para migraciones por CLI, instalar una vez `dotnet-ef` en versión compatible:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12
```

## PostgreSQL y pgAdmin con Docker Compose

Crear `compose.yaml` en la raíz del repositorio:

```yaml
services:
  database:
    image: postgres:18.6
    restart: unless-stopped
    environment:
      POSTGRES_DB: fepp_contaops_dev
      POSTGRES_USER: app
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?Define POSTGRES_PASSWORD en .env}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U app -d fepp_contaops_dev"]
      interval: 5s
      timeout: 5s
      retries: 10

  pgadmin:
    image: dpage/pgadmin4:latest
    restart: unless-stopped
    environment:
      PGADMIN_DEFAULT_EMAIL: ${PGADMIN_DEFAULT_EMAIL:?Define PGADMIN_DEFAULT_EMAIL en .env}
      PGADMIN_DEFAULT_PASSWORD: ${PGADMIN_DEFAULT_PASSWORD:?Define PGADMIN_DEFAULT_PASSWORD en .env}
    ports:
      - "5050:80"
    volumes:
      - pgadmin_data:/var/lib/pgadmin
    depends_on:
      database:
        condition: service_healthy

volumes:
  postgres_data:
  pgadmin_data:
```

Crear `.env` junto a `compose.yaml` con credenciales locales únicas y no subirlo al repositorio:

```dotenv
POSTGRES_PASSWORD=definir_una_clave_local
PGADMIN_DEFAULT_EMAIL=admin@fepp.local
PGADMIN_DEFAULT_PASSWORD=definir_otra_clave_local
```

pgAdmin se ejecuta en Docker, no necesita instalación en el equipo. Abrir `http://localhost:5050`. Al registrar la conexión en pgAdmin, usar:

- Host: `database`
- Puerto: `5432`
- Mantenimiento: `fepp_contaops_dev`
- Usuario: `app`
- Contraseña: el valor `POSTGRES_PASSWORD` del `.env`

La API que se inicia con `dotnet run` se ejecuta en el host; su conexión debe usar `Host=localhost;Port=5432`. Los contenedores se comunican usando el nombre del servicio `database`.

> Para entornos compartidos, fijar una versión concreta de pgAdmin en vez de `latest` y gestionar las contraseñas fuera del repositorio.

## Configurar secretos locales

Desde la raíz del repositorio, inicializar User Secrets para Api y configurar la conexión, clave de firma JWT y ruta absoluta al directorio `Plan_Cuentas` proporcionado por FEPP:

```powershell
dotnet user-secrets init --project src/Fepp.ContaOps.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=fepp_contaops_dev;Username=app;Password=VALOR_DE_POSTGRES_PASSWORD" --project src/Fepp.ContaOps.Api
dotnet user-secrets set "Jwt:SigningKey" "REEMPLAZAR_POR_UN_SECRETO_ALEATORIO_LARGO" --project src/Fepp.ContaOps.Api
dotnet user-secrets set "FileImport:RootPath" "RUTA_ABSOLUTA_AL_DIRECTORIO_Plan_Cuentas" --project src/Fepp.ContaOps.Api
```

`FileImport:RootPath` debe apuntar al directorio raíz que contiene los archivos Excel y PDF. El nombre de la clave se mantiene estable; su valor es la ruta específica del ambiente. No poner la ruta real en `appsettings.json`, `.env`, el código fuente ni Git. En Windows, usar la ruta real entre comillas, por ejemplo `D:\FEPP\Contabilidad\Plan_Cuentas`.

Si la API se ejecuta dentro de Docker, montar el directorio anfitrión de solo lectura, por ejemplo `./Plan_Cuentas:/data/Plan_Cuentas:ro`, y establecer el secreto `FileImport:RootPath` con `/data/Plan_Cuentas`. El secreto debe reflejar la ruta visible desde el contenedor.

## Logging JSON

En `Program.cs`, configurar el proveedor JSON de consola para que el proceso escriba logs estructurados a `stdout`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole();
``` 

Aplicar redacción de forma explícita a valores clasificados antes de registrarlos; instalar el paquete no filtra por sí solo cualquier mensaje. No registrar cuerpos HTTP ni valores secretos.

## Migraciones e inicio

Crear/versionar la migración inicial cuando existan el `DbContext` y el modelo. Ejecutar estos comandos desde la raíz:

```powershell
dotnet ef migrations add InitialCreate --project src/Fepp.ContaOps.Infrastructure --startup-project src/Fepp.ContaOps.Api --output-dir Persistence/Migrations
```

Para descargar e iniciar un repositorio existente:

```powershell
git clone URL_GIT_DEL_REPOSITORIO FEPP-ContaOps
Set-Location FEPP-ContaOps
docker compose up -d database pgadmin
dotnet restore FEPP-ContaOps.slnx
dotnet build FEPP-ContaOps.slnx
dotnet ef database update --project src/Fepp.ContaOps.Infrastructure --startup-project src/Fepp.ContaOps.Api
dotnet run --project src/Fepp.ContaOps.Api
```

Antes de iniciar la API, crear `.env` y configurar User Secrets como se indica arriba. Abrir Swagger en `https://localhost:<puerto>/swagger` y pgAdmin en `http://localhost:5050`. Para detener los servicios manteniendo sus datos:

```powershell
docker compose down
```

Para eliminar también los datos persistidos en los volúmenes (operación destructiva):

```powershell
docker compose down --volumes
```

## Referencias

- [Documentación de PostgreSQL en Docker](https://hub.docker.com/_/postgres)
- [Documentación de pgAdmin en Docker](https://www.pgadmin.org/docs/pgadmin4/latest/container_deployment.html)
- [Logging en ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/logging/?view=aspnetcore-10.0)
- [Redacción de datos en .NET](https://learn.microsoft.com/dotnet/core/extensions/data-redaction)
- [Naming y documentación del código](dotnet-naming-and-code-documentation.md)