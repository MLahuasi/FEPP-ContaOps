namespace Fepp.ContaOps.Domain.Users;

/// <summary>
/// Usuario autenticable independiente del proveedor técnico de identidad.
/// </summary>
public sealed class User
{
    private readonly IReadOnlyCollection<string> _roles;

    /// <summary>
    /// Inicializa un usuario de dominio.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="email">Correo electrónico del usuario.</param>
    /// <param name="roles">Roles efectivos del usuario.</param>
    public User(UserId id, string email, IEnumerable<string> roles)
    {
        if (!id.IsValid())
        {
            throw new ArgumentException("A user identifier is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("A user email is required.", nameof(email));
        }

        ArgumentNullException.ThrowIfNull(roles);

        Id = id;
        Email = email;
        _roles = roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Obtiene el identificador del usuario.
    /// </summary>
    public UserId Id { get; }

    /// <summary>
    /// Obtiene el correo electrónico del usuario.
    /// </summary>
    public string Email { get; }

    /// <summary>
    /// Obtiene los roles efectivos del usuario.
    /// </summary>
    public IReadOnlyCollection<string> Roles => _roles;
}
