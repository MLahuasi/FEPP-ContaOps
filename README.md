# FEPP-ContaOps

FEPP-ContaOps es una plataforma de la Fundación FEPP para reducir la carga operativa de procesos manuales del área contable. El módulo inicial administra regionales e importa planes de cuentas desde archivos PDF y Excel.

## Arquitectura y stack

La solución usa arquitectura hexagonal y DDD, separada en proyectos .NET por responsabilidad:

```text
src/
├── Fepp.ContaOps.Domain/          # Modelo y reglas del negocio
├── Fepp.ContaOps.Application/     # Casos de uso y puertos
├── Fepp.ContaOps.Infrastructure/  # Persistencia y adaptadores externos
└── Fepp.ContaOps.Api/             # HTTP, DTOs, validación, Swagger y composición
```

Stack principal: .NET 10, ASP.NET Core Web API, PostgreSQL 18 en Docker, Entity Framework Core con Npgsql, Identity y JWT, FluentValidation, Swagger, PdfPig y ClosedXML. Los paquetes NuGet por proyecto y los comandos para crear la solución están en [create-install.md](./sdd/create-install.md).

Documentación complementaria:

- [Arquitectura de la Web API](./sdd/webapi-architecture.md)
- [Modelo e importación del balance global](./sdd/global-balance-sheet.md)
- [Naming y documentación de código](./sdd/dotnet-naming-and-code-documentation.md)

## Desarrollo asistido

Context7 y el agente `spec-acceptance-validator` se usan únicamente durante el desarrollo con OpenCode; no son dependencias de ejecución de la API.

Para habilitarlos, crear la variable de entorno de Windows fuera del repositorio:

```powershell
[Environment]::SetEnvironmentVariable('CONTEXT7_API_KEY', 'YOUR_CONTEXT7_TOKEN', 'User')
```

Cerrar y volver a abrir la terminal y OpenCode después de crearla. El `opencode.json` del proyecto consume la variable como `{env:CONTEXT7_API_KEY}`. No guardar el token en archivos versionados ni en `.env`.

## Iniciar la API local

Requisitos: SDK .NET 10, Git y Docker con Docker Compose. Desde la raíz del repositorio:

1. Crear `.env` con las variables `POSTGRES_PASSWORD`, `PGADMIN_DEFAULT_EMAIL` y `PGADMIN_DEFAULT_PASSWORD`. No versionar este archivo.
2. Configurar User Secrets en `Fepp.ContaOps.Api`: cadena de conexión, clave JWT y `FileImport:RootPath` con la ruta real al directorio `Plan_Cuentas` de FEPP. Los comandos están en [create-install.md](./sdd/create-install.md).
3. Iniciar PostgreSQL y pgAdmin en Docker:

   ```powershell
   docker compose up -d database pgadmin
   ```

4. Restaurar, compilar, aplicar migraciones e iniciar la API:

   ```powershell
   dotnet restore FEPP-ContaOps.slnx
   dotnet build FEPP-ContaOps.slnx
   dotnet ef database update --project src/Fepp.ContaOps.Infrastructure --startup-project src/Fepp.ContaOps.Api
   dotnet run --project src/Fepp.ContaOps.Api
   ```

Swagger estará en `https://localhost:<puerto>/swagger`; pgAdmin estará en `http://localhost:5050`. Desde pgAdmin, registrar el servidor con host `database`, puerto `5432` y las credenciales del `.env`. Para detener los contenedores, ejecutar `docker compose down`; los volúmenes conservan los datos.

La API se ejecuta localmente en este flujo, por lo que `FileImport:RootPath` debe ser una ruta accesible desde el equipo. Si la API se despliega en un contenedor, montar el directorio compartido en modo solo lectura y establecer el secreto con la ruta interna del contenedor.

## Base de datos e importación

PostgreSQL usa la imagen oficial Docker `postgres:18.6`. El modelo de regionales, archivos y cuentas, junto con sus reglas de importación, está descrito en [global-balance-sheet.md](./sdd/global-balance-sheet.md). La ruta raíz de lectura de PDFs y Excel se configura como secreto `FileImport:RootPath`; no debe quedar fija en el código ni en archivos versionados.

## Trazabilidad y protección de datos

Usar `ILogger<T>` con eventos estructurados y salida JSON a consola; Docker o la plataforma de despliegue recoge los logs y los envía al centralizador. No guardar logs técnicos en la base operativa. Omitir credenciales, tokens, archivos y datos contables innecesarios. La guía de instalación incluye el paquete de redacción de datos sensibles y pautas para configurar reglas por ambiente.

## Convenciones

Aplicar [dotnet-naming-and-code-documentation.md](./sdd/dotnet-naming-and-code-documentation.md) para nombres y comentarios XML del código.
