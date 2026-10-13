# Convenciones de nombres y documentación de código

## Propósito

Esta guía define las convenciones de nombres y comentarios XML para los proyectos .NET de la solución. Se basa en las convenciones de C# y .NET de Microsoft; el requisito de documentar también los miembros privados es una regla del proyecto.

## Convenciones de nombres

| Elemento | Convención | Ejemplo |
|---|---|---|
| Namespace | PascalCase; refleja el proyecto y el área funcional | `Acme.Api.Users` |
| Clase, record, struct, enum, delegate | PascalCase, nombre que describa el concepto | `UserService`, `RegisterUserRequest` |
| Interface | PascalCase con prefijo `I` | `IUserRepository` |
| Método o función | PascalCase; verbo o frase verbal | `GetUserByIdAsync` |
| Propiedad y evento | PascalCase | `Email`, `UserRegistered` |
| Parámetro | camelCase, nombre descriptivo | `userId`, `cancellationToken` |
| Variable local | camelCase | `currentUser` |
| Campo privado o interno de instancia | camelCase con prefijo `_` | `_userRepository` |
| Constante | PascalCase | `DefaultPageSize` |
| Parámetro genérico | `T` o `T` seguido de un nombre descriptivo | `T`, `TEntity` |
| Método asíncrono | Añade el sufijo `Async` | `SaveChangesAsync` |
| Valor booleano | Nombre afirmativo y claro | `IsActive`, `HasPermission`, `CanEdit` |
| Enum | PascalCase y nombre singular; plural si representa una combinación de flags | `OrderStatus`, `FilePermissions` |

### Reglas complementarias

- Usar nombres completos y significativos. Evitar abreviaturas, salvo las conocidas en el ecosistema .NET, como `HTTP`, `JSON` o `ID`.
- En nombres PascalCase, las siglas de dos letras se escriben en mayúsculas (`IOStream`); las siglas más largas siguen el uso PascalCase (`XmlReader`, `JsonSerializer`).
- Mantener el mismo nombre de parámetro al implementar o sobrescribir un miembro.
- No agregar prefijos como `C` a las clases ni sufijos redundantes como `Impl` a una implementación.
- Las interfaces expresan una capacidad o contrato y usan el prefijo `I`; por ejemplo, `IUserRepository`.
- Los métodos expresan acciones. Las propiedades expresan datos o estado y no deben simular una operación con efectos secundarios.
- Las clases de excepción terminan en `Exception`; las clases derivadas de `EventArgs` terminan en `EventArgs`.
- Los nombres de archivos deben coincidir con el tipo principal definido en ellos. Un tipo por archivo es la convención preferida, salvo tipos pequeños estrechamente relacionados.

## Comentarios y documentación XML

### Regla del proyecto

Se deben documentar todas las clases, interfaces, métodos, constructores y sus parámetros, incluidos los miembros privados. También se deben documentar las propiedades públicas y los tipos genéricos. La documentación debe describir el propósito, contrato o comportamiento que no resulte evidente por la firma.

Use comentarios XML con `///` inmediatamente antes de la declaración. Para métodos y constructores, agregue un `<param>` por cada parámetro, con el nombre exactamente igual al de la firma.

### Etiquetas XML

| Etiqueta | Uso |
|---|---|
| `<summary>` | Propósito del tipo o miembro. Obligatoria para cada clase, interfaz y método. |
| `<param name="...">` | Propósito, formato, límites o significado de cada parámetro de método y constructor. |
| `<typeparam name="...">` | Propósito de cada parámetro genérico. |
| `<returns>` | Significado del valor devuelto por métodos que retornan un valor. |
| `<value>` | Significado de una propiedad documentada. |
| `<exception cref="...">` | Excepciones relevantes que el miembro puede producir y sus condiciones. |
| `<remarks>` | Detalles adicionales del contrato que no caben en el resumen. |
| `<paramref name="..."/>` | Referencia a un parámetro dentro del texto XML. |
| `<inheritdoc/>` | Reutilizar la documentación de un contrato base o interfaz cuando sea realmente equivalente. |

No agregue `<returns>` en métodos `void`. Documente excepciones que formen parte del comportamiento previsto; no enumere excepciones genéricas que el método no controla. En una implementación, puede utilizar `<inheritdoc/>` cuando herede por completo el contrato documentado de una interfaz.

### Ejemplo

```csharp
/// <summary>
/// Busca un usuario por su identificador.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Devuelve el usuario asociado al identificador indicado, si existe.
    /// </summary>
    /// <param name="userId">Identificador único del usuario que se busca.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>El usuario encontrado o <see langword="null"/> si no existe.</returns>
    Task<User?> GetByIdAsync(
        UserId userId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Proporciona operaciones de aplicación relacionadas con usuarios.
/// </summary>
public sealed class UserService
{
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Inicializa el servicio de usuarios.
    /// </summary>
    /// <param name="userRepository">Repositorio utilizado para consultar usuarios.</param>
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Obtiene un usuario por su identificador.
    /// </summary>
    /// <param name="userId">Identificador único del usuario que se busca.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>El usuario encontrado o <see langword="null"/> si no existe.</returns>
    public Task<User?> GetByIdAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        return _userRepository.GetByIdAsync(userId, cancellationToken);
    }
}
```

### Comentarios dentro de la implementación

- Preferir nombres y estructura de código claros antes que comentarios que repitan la instrucción.
- Usar `//` para explicar el motivo de una decisión no obvia, una limitación técnica o una condición que el código por sí solo no muestre.
- No comentar cada línea ni describir literalmente lo que ya expresa el código.
- Actualizar o eliminar comentarios cuando cambia el comportamiento. Un comentario desactualizado es más perjudicial que no tenerlo.
- Los comentarios `TODO` deben indicar una acción concreta y, cuando exista, enlazar el issue o tarea que la seguirá.

## Verificación

En los archivos `.csproj`, se puede activar la generación de documentación XML:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

Esto genera el archivo XML de documentación y permite que el compilador informe miembros públicos sin comentario XML mediante CS1591. Ese diagnóstico no verifica por sí solo todos los miembros privados exigidos por esta guía; estos también deben revisarse durante el desarrollo y la revisión del código.

La documentación XML debe compilar correctamente: usar etiquetas válidas, nombres de parámetros que coincidan con la firma y descripciones que reflejen el comportamiento actual.

## Referencias oficiales

- [Convenciones de nombres de identificadores de C#](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/identifier-names)
- [Reglas de nombres de .NET](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/naming-rules)
- [Comentarios de documentación XML de C#](https://learn.microsoft.com/dotnet/csharp/language-reference/xmldoc/)
- [Etiquetas recomendadas para documentación XML](https://learn.microsoft.com/dotnet/csharp/language-reference/xmldoc/recommended-tags)
- [Advertencia CS1591](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/cs1591)
