# Arquitectura de la Web API

## Propósito

Esta arquitectura organiza la Web API en proyectos separados por responsabilidad. Aplica arquitectura hexagonal y Domain-Driven Design (DDD). Cada operación se implementa como un caso de uso explícito.

La selección del motor de base de datos y el modelo de datos se describen en `README.md`.

## Estructura de la solución

```text
<MyApi>.sln
└── src/
    ├── <MyApi>.Domain/
    ├── <MyApi>.Application/
    ├── <MyApi>.Infrastructure/
    └── <MyApi>.Api/
```

`<MyApi>` se reemplaza por el nombre de la solución.

## Proyectos y responsabilidades

### `<MyApi>.Domain`

Contiene el modelo y las reglas del negocio. Es independiente de ASP.NET Core, Entity Framework, JWT, proveedores de base de datos y otros proyectos de la solución.

```text
<MyApi>.Domain/
├── Users/
│   ├── User.cs
│   └── UserId.cs
├── Courses/
│   ├── Course.cs
│   └── CourseId.cs
└── <MyApi>.Domain.csproj
```

Incluye entidades, agregados, objetos de valor, invariantes y servicios de dominio cuando sean necesarios. Las entidades y agregados protegen las reglas del negocio mediante sus métodos.

**Dependencias NuGet:** ninguna inicialmente.

### `<MyApi>.Application`

Coordina los casos de uso y define las interfaces que necesita para interactuar con el exterior. Depende de Domain, pero no de Api ni de las implementaciones de Infrastructure.

```text
<MyApi>.Application/
├── Users/
│   ├── RegisterUser/
│   │   ├── RegisterUser.cs
│   │   └── RegisterUserInput.cs
│   └── LoginUser/
│       ├── LoginUser.cs
│       └── LoginUserInput.cs
├── Courses/
│   └── CreateCourse/
│       ├── CreateCourse.cs
│       └── CreateCourseInput.cs
├── Abstractions/
│   ├── IUserRepository.cs
│   ├── IUserCredentialService.cs
│   └── ITokenIssuer.cs
└── <MyApi>.Application.csproj
```

Cada caso de uso tiene una responsabilidad, un modelo de entrada propio y un resultado explícito. Application define puertos como:

- `IUserRepository`: consultar y guardar usuarios.
- `IUserCredentialService`: crear credenciales o verificar contraseñas.
- `ITokenIssuer`: emitir un token para un usuario autenticado.

Los casos de uso no reciben DTOs HTTP ni dependen de `HttpContext`, `DbContext`, `UserManager` o tipos concretos de JWT.

**Dependencias NuGet:** ninguna obligatoria inicialmente.

### `<MyApi>.Infrastructure`

Implementa los puertos de Application y contiene los adaptadores técnicos: persistencia, identidad, emisión de tokens y conexiones con servicios externos.

```text
<MyApi>.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/
│   ├── Repositories/
│   └── Migrations/
├── Identity/
├── Security/
│   └── JwtTokenIssuer.cs
├── DependencyInjection.cs
└── <MyApi>.Infrastructure.csproj
```

Infrastructure implementa repositorios, configura el acceso a datos y registra sus servicios. Si se usa ASP.NET Core Identity, su almacenamiento y adaptador se ubican aquí. La creación y firma de tokens también puede implementarse aquí detrás de `ITokenIssuer`.

**Dependencias NuGet de datos:**

- `Microsoft.EntityFrameworkCore` para el acceso ORM.
- `Microsoft.EntityFrameworkCore.Design` para las tareas de diseño y migraciones; marcar como dependencia privada de desarrollo.
- El proveedor EF Core del motor seleccionado, según lo documentado en `README.md`.
- `Microsoft.EntityFrameworkCore.Tools` solo si se usarán las herramientas de Package Manager Console de Visual Studio. Con la CLI, instalar la herramienta `dotnet-ef` en una versión compatible con EF Core.
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` solo si se elige ASP.NET Core Identity.
- `System.IdentityModel.Tokens.Jwt` si este adaptador construye y firma los JWT.

Las dependencias del proveedor concreto de base de datos y los detalles del modelo se especifican en `README.md`.

### `<MyApi>.Api`

Es el ejecutable y adaptador HTTP. Contiene controladores, DTOs de transporte, validación de requests, autenticación, autorización, middleware y composición de dependencias.

```text
<MyApi>.Api/
├── Controllers/
├── Contracts/
│   ├── Requests/
│   └── Responses/
├── Validation/
├── Middleware/
├── Extensions/
├── Program.cs
└── <MyApi>.Api.csproj
```

Los controladores convierten solicitudes HTTP en llamadas a casos de uso y resultados de aplicación en respuestas HTTP. No contienen reglas del negocio ni consultas directas a la base de datos.

**Dependencias NuGet:**

- `FluentValidation.DependencyInjectionExtensions` si se elige FluentValidation para los DTOs.
- `Microsoft.AspNetCore.Authentication.JwtBearer` si se validan tokens Bearer.
- Un paquete OpenAPI/Swagger es opcional.

## Referencias entre proyectos

```text
<MyApi>.Domain
      ↑
<MyApi>.Application
      ↑
<MyApi>.Infrastructure

<MyApi>.Api ───────────────→ <MyApi>.Application
<MyApi>.Api ───────────────→ <MyApi>.Infrastructure
```

- Domain no referencia otros proyectos de la solución.
- Application referencia Domain.
- Infrastructure referencia Application y Domain.
- Api referencia Application e Infrastructure y actúa como raíz de composición.
- Las capas internas no dependen de las capas externas.
- Application define las interfaces; Infrastructure las implementa.

## DTOs de entrada y salida

Los DTOs HTTP se ubican en Api, dentro de `Contracts/Requests` y `Contracts/Responses`. Definen el contrato público de la API; no representan las entidades de dominio ni el esquema de datos.

- Un request DTO contiene únicamente los campos que el cliente puede enviar.
- Un response DTO contiene únicamente los campos que la API debe devolver.
- No exponer directamente entidades de dominio o modelos de persistencia en las respuestas.
- No permitir que el cliente establezca campos internos o controlados por el servidor, como roles privilegiados o identificadores generados internamente.
- Application no debe referenciar tipos definidos en Api. Cada caso de uso recibe su propio modelo de entrada, por ejemplo `RegisterUserInput`.
- El controlador convierte el request DTO al modelo de entrada de Application y convierte el resultado al response DTO. El mapeo puede ser manual; agregar una biblioteca de mapeo solo si reduce duplicación real.
- Agrupar parámetros de ruta o query en un DTO cuando tengan varias propiedades o reglas propias.

```text
JSON y parámetros HTTP
        ↓
Request DTO de Api
        ↓ validación del contrato
Modelo de entrada de Application
        ↓
Caso de uso
        ↓
Resultado de Application
        ↓ mapeo
Response DTO de Api
        ↓
Respuesta HTTP
```

## Validaciones de entrada

La validación se realiza antes de ejecutar el caso de uso y en niveles con responsabilidades distintas.

### Validación del DTO en Api

Valida la forma de la solicitud HTTP:

- Campos obligatorios.
- Formato de valores, como email, fecha o identificador.
- Longitudes, rangos y límites permitidos.
- Presencia y formato de parámetros de ruta, query y body.

Para reglas sencillas se pueden usar Data Annotations y `[ApiController]`, que devuelve `400 Bad Request` cuando el modelo no es válido. Para reglas más expresivas, se puede usar FluentValidation:

- Instalar `FluentValidation.DependencyInjectionExtensions` en `<MyApi>.Api`.
- Mantener los validadores junto a los DTOs, en `Api/Validation` o junto a cada feature.
- Registrar los validadores e invocarlos explícitamente antes del caso de uso, por ejemplo mediante un filtro de acción.
- Traducir los errores a una respuesta `400 Bad Request` con un formato uniforme.

La integración debe ejecutar efectivamente los validadores; instalar el paquete por sí solo no activa automáticamente la validación de los controladores.

### Validación del caso de uso

Application valida precondiciones que dependen de la operación o del estado del sistema, por ejemplo:

- Que el email no esté registrado.
- Que el recurso exista antes de modificarlo.
- Que una transición esté permitida para la operación solicitada.

Application devuelve errores de aplicación explícitos. No debe depender de `ModelState` ni producir respuestas HTTP directamente.

### Invariantes del dominio

Domain protege las reglas que siempre deben cumplirse, independientemente del endpoint o adaptador: estado válido de agregados, transiciones permitidas y restricciones entre sus valores. Estas reglas se implementan en entidades, agregados u objetos de valor, sin depender de atributos de ASP.NET ni de una biblioteca de validación.

Api traduce errores de Application y Domain al código y formato HTTP definido para cada operación.

## Recorrido de una solicitud

```text
Cliente HTTP
    ↓
Controller de <MyApi>.Api
    ↓ DTO y validación de entrada
Caso de uso de <MyApi>.Application
    ↓ interfaces definidas en Application
Adaptadores de <MyApi>.Infrastructure
    ↓
Almacenamiento o servicio externo configurado
```

## Autenticación JWT

### Inicio de sesión

1. El controller recibe un request DTO con las credenciales y valida su formato.
2. Invoca el caso de uso `LoginUser`.
3. Application solicita comprobar las credenciales mediante `IUserCredentialService`.
4. Infrastructure consulta al proveedor de identidad y verifica la contraseña.
5. Si la autenticación es correcta, Application solicita un token mediante `ITokenIssuer`.
6. Infrastructure firma el JWT y devuelve el resultado.
7. Api construye el response DTO.

### Solicitudes autenticadas

1. El cliente envía `Authorization: Bearer <token>`.
2. El middleware JWT de ASP.NET Core valida firma y expiración; issuer y audience se validan según la configuración del sistema.
3. Authorization aplica las políticas o roles configurados para el endpoint.
4. El controller invoca el caso de uso autorizado.

Api configura y valida el esquema Bearer. Infrastructure puede implementar la creación y firma del token detrás de `ITokenIssuer`. Las claves se obtienen de la configuración segura del ambiente.

## Configuración

La configuración se separa por ambiente. Claves, credenciales y cadenas de conexión se suministran desde variables de entorno o un gestor de secretos; no se guardan en archivos versionados.

## Reglas arquitectónicas

- **ARCH-001:** La solución separa Domain, Application, Infrastructure y Api en proyectos distintos.
- **ARCH-002:** Domain no depende de frameworks web, proveedores de datos ni otros proyectos de la solución.
- **ARCH-003:** Application representa cada operación como un caso de uso explícito.
- **ARCH-004:** Application define las interfaces que necesita para interactuar con sistemas externos.
- **ARCH-005:** Infrastructure implementa esas interfaces; las capas internas no dependen de sus implementaciones.
- **ARCH-006:** Api define DTOs de entrada y salida y no expone directamente entidades del dominio.
- **ARCH-007:** Api valida los DTOs antes de invocar un caso de uso.
- **ARCH-008:** Application valida precondiciones del caso de uso y Domain protege las invariantes del negocio.
- **ARCH-009:** Api traduce errores de validación y aplicación a respuestas HTTP uniformes.
- **ARCH-010:** El modelo de datos y el proveedor concreto de base de datos se documentan en `README.md`.

## Referencias

- [ASP.NET Core: validación de modelos](https://learn.microsoft.com/aspnet/core/mvc/models/validation)
- [FluentValidation: integración con ASP.NET](https://docs.fluentvalidation.net/en/latest/aspnet.html)
