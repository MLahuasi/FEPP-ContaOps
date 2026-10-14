# Implementar 01-base-api-arquitectura

## PASOS

### 1. Crear solución, cuatro proyectos productivos y dos proyectos de pruebas.

#### Arquitectura Hexagonal y DDD — Relación entre proyectos

La solución `FEPP-ContaOps` utiliza **Arquitectura Hexagonal (Ports & Adapters)** y **Domain-Driven Design (DDD)** para separar la lógica de negocio, los casos de uso y las implementaciones técnicas.

##### Diagrama de dependencias

```mermaid
flowchart TB
    API["Fepp.ContaOps.Api<br/>Presentación / Composition Root"]
    APP["Fepp.ContaOps.Application<br/>Casos de uso / Puertos"]
    INF["Fepp.ContaOps.Infrastructure<br/>Adaptadores"]
    DOM["Fepp.ContaOps.Domain<br/>Dominio"]

    API --> APP
    API --> INF
    INF --> APP
    INF --> DOM
    APP --> DOM

    style API fill:#34495e,color:#fff
    style APP fill:#2471a3,color:#fff
    style INF fill:#566573,color:#fff
    style DOM fill:#196f3d,color:#fff
```

Las flechas representan referencias entre proyectos (`ProjectReference`), no el flujo de ejecución.

##### Responsabilidades y dependencias

| Proyecto           | Responsabilidad                                                                    | Depende de                  |
| ------------------ | ---------------------------------------------------------------------------------- | --------------------------- |
| **Domain**         | Entidades, Value Objects, Aggregates y reglas de negocio (DDD).                    | Ninguno                     |
| **Application**    | Casos de uso, orquestación y puertos (interfaces).                                 | Domain                      |
| **Infrastructure** | Adaptadores técnicos: repositorios, persistencia, correo e integraciones externas. | Application, Domain         |
| **API**            | Endpoints HTTP, controladores y configuración de dependencias (Composition Root).  | Application, Infrastructure |

##### Justificación arquitectónica

| Principio                        | Aplicación                                                                                                            |
| -------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| **DDD**                          | Domain concentra el modelo y las reglas del negocio.                                                                  |
| **Arquitectura Hexagonal**       | Application define puertos e Infrastructure implementa adaptadores secundarios; API funciona como adaptador primario. |
| **Dependency Inversion (DIP)**   | Los casos de uso dependen de abstracciones, no de implementaciones técnicas.                                          |
| **Separation of Concerns (SoC)** | Cada proyecto tiene responsabilidades claramente delimitadas.                                                         |
| **Bajo acoplamiento**            | Permite sustituir implementaciones sin modificar los casos de uso.                                                    |
| **Testabilidad**                 | Facilita probar la lógica de negocio independientemente de la infraestructura.                                        |

##### Reglas de dependencia

1. **Domain** no depende de ningún otro proyecto de la solución.
2. **Application** depende únicamente de Domain.
3. **Infrastructure** implementa los puertos definidos en Application o Domain.
4. **API** integra Application e Infrastructure mediante inyección de dependencias.
5. Las dependencias del núcleo apuntan hacia Domain, nunca hacia Infrastructure.

**Conclusión:** la estructura protege la lógica de negocio frente a cambios tecnológicos y permite mantener una aplicación modular, testeable y escalable sin introducir complejidad innecesaria.

---

### 2. Configurar referencias e inyección de dependencias.

#### Registro de dependencias (DependencyInjection)

En **Clean Architecture**, las clases `DependencyInjection` permiten centralizar el registro de servicios y sus implementaciones mediante el mecanismo de **inyección de dependencias (Dependency Injection, DI)** de .NET.

Cada capa encapsula sus propios registros, mientras que la capa **API actúa como Composition Root**, integrando las dependencias necesarias para ejecutar la aplicación.

#### Comparación entre Application e Infrastructure

| Característica                     | Application                                          | Infrastructure                                                                           |
| ---------------------------------- | ---------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| **Clase**                          | `DependencyInjection`                                | `DependencyInjection`                                                                    |
| **Ubicación**                      | `Fepp.ContaOps.Application`                          | `Fepp.ContaOps.Infrastructure`                                                           |
| **Método**                         | `AddApplication()`                                   | `AddInfrastructure()`                                                                    |
| **Propósito**                      | Registrar servicios y casos de uso de Application.   | Registrar adaptadores e implementaciones técnicas de Infrastructure.                     |
| **Dependencias de .NET**           | `Microsoft.Extensions.DependencyInjection`           | `Microsoft.Extensions.DependencyInjection` y `Microsoft.Extensions.Configuration`        |
| **Parámetros**                     | `IServiceCollection services`                        | `IServiceCollection services`, `IConfiguration configuration`                            |
| **Servicios registrados**          | Casos de uso, validadores y servicios de aplicación. | Repositorios, persistencia, correo electrónico, almacenamiento e integraciones externas. |
| **Ejemplo**                        | `services.AddScoped<ValidateBalanceUseCase>();`      | `services.AddScoped<IBalanceRepository, SqlBalanceRepository>();`                        |
| **Configuración externa**          | Generalmente no requiere `IConfiguration`.           | Utiliza `IConfiguration` para conexiones y parámetros de servicios externos.             |
| **Responsabilidad arquitectónica** | Encapsular los registros de la lógica de aplicación. | Vincular abstracciones con implementaciones técnicas concretas.                          |
| **Invocación desde API**           | `builder.Services.AddApplication();`                 | `builder.Services.AddInfrastructure(builder.Configuration);`                             |
| **Valor de retorno**               | `IServiceCollection`                                 | `IServiceCollection`                                                                     |

#### Dependencias utilizadas

| Dependencia                                | Descripción                                                                                                         | Utilización                           |
| ------------------------------------------ | ------------------------------------------------------------------------------------------------------------------- | ------------------------------------- |
| `Microsoft.Extensions.DependencyInjection` | Proporciona las abstracciones y métodos de registro de servicios del sistema de DI de .NET.                         | Application e Infrastructure.         |
| `Microsoft.Extensions.Configuration`       | Proporciona las abstracciones para acceder a la configuración de la aplicación.                                     | Infrastructure.                       |
| `IServiceCollection`                       | Colección que almacena los registros de servicios y sus ciclos de vida.                                             | Parámetro principal de ambos métodos. |
| `IConfiguration`                           | Interfaz que permite consultar configuraciones provenientes de archivos JSON, variables de entorno y otras fuentes. | Parámetro de `AddInfrastructure()`.   |

#### Ejemplos de implementación

[**Application — DependencyInjection.cs**](../../src/Fepp.ContaOps.Application/DependencyInjection.cs)

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Fepp.ContaOps.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<ValidateBalanceUseCase>();

        return services;
    }
}
```

Este método registra los servicios propios de Application.

[**Infrastructure — DependencyInjection.cs**](../../src/Fepp.ContaOps.Infrastructure/DependencyInjection.cs)

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fepp.ContaOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<
            IBalanceRepository,
            SqlBalanceRepository>();

        return services;
    }
}
```

Este método registra implementaciones concretas de los contratos utilizados por Application o Domain. El parámetro `configuration` permite incorporar configuraciones externas cuando sean necesarias.

**API — Program.cs**

```csharp
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);
```

La capa API invoca ambos métodos para configurar los servicios que utilizará la aplicación.

#### ¿Por qué se implementan?

Las clases `DependencyInjection` permiten:

1. **Separación de responsabilidades:** cada capa centraliza el registro de sus propios servicios.
2. **Bajo acoplamiento:** Application depende de abstracciones, sin conocer las implementaciones de Infrastructure.
3. **Mantenibilidad:** los nuevos servicios se registran en su capa correspondiente, evitando modificar continuamente `Program.cs`.
4. **Modularidad:** cada proyecto encapsula su configuración de dependencias.
5. **Testabilidad:** las dependencias pueden sustituirse por implementaciones simuladas durante las pruebas.
6. **Configuración centralizada:** Infrastructure puede utilizar parámetros externos sin incorporarlos directamente en los casos de uso.

#### Conceptos relacionados

| Concepto                                 | Descripción                                                                                                                                    |
| ---------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| **Dependency Injection (DI)**            | Técnica que proporciona las dependencias de una clase desde el exterior, habitualmente mediante su constructor.                                |
| **Dependency Inversion Principle (DIP)** | Principio SOLID que establece que los módulos de alto nivel no deben depender de módulos de bajo nivel; ambos deben depender de abstracciones. |
| **Extension Method**                     | Método estático que utiliza `this` en su primer parámetro para permitir su invocación mediante la sintaxis de instancia.                       |
| **Composition Root**                     | Punto donde se integran los registros y se configura la construcción de dependencias. Normalmente se encuentra en API (`Program.cs`).          |
| **AddScoped**                            | Registra una instancia compartida dentro de un ámbito; en aplicaciones ASP.NET Core, generalmente una petición HTTP.                           |
| **AddTransient**                         | Crea una nueva instancia cada vez que se solicita el servicio.                                                                                 |
| **AddSingleton**                         | Mantiene una instancia compartida durante la vida del contenedor.                                                                              |

**Conclusión:** `DependencyInjection` permite organizar la configuración del contenedor por capas, manteniendo las responsabilidades delimitadas. Application registra sus casos de uso, Infrastructure proporciona las implementaciones técnicas y API realiza la composición final. Esta organización favorece la mantenibilidad, la modularidad y la aplicación del principio de inversión de dependencias.
