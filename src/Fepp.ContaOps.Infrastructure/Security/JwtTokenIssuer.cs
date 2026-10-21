using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fepp.ContaOps.Infrastructure.Security;

/// <summary>
/// Emite tokens JWT firmados con una clave secreta configurada en el ambiente.
/// </summary>
public sealed class JwtTokenIssuer : ITokenIssuer
{
    /// <summary>
    /// Opciones validadas utilizadas para construir los tokens.
    /// </summary>
    private readonly JwtOptions _options;

    /// <summary>
    /// Inicializa el emisor JWT.
    /// </summary>
    /// <param name="options">Opciones JWT validadas.</param>
    public JwtTokenIssuer(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Emite un JWT con la identidad y los roles del usuario autenticado.
    /// </summary>
    /// <param name="user">Usuario autenticado.</param>
    /// <param name="sessionId">Identificador de la sesión asociada al token.</param>
    /// <param name="jti">Identificador único del token.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Token firmado y duración en segundos.</returns>
    public Task<TokenResult> IssueAsync(
        User user,
        Guid sessionId,
        string jti,
        CancellationToken cancellationToken = default)
    {
        DateTime issuedAt = DateTime.UtcNow;
        DateTime expiresAt = issuedAt.AddMinutes(_options.AccessTokenLifetimeMinutes);
        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.Value),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        ];
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        SymmetricSecurityKey signingKey = new(
            Encoding.UTF8.GetBytes(_options.SigningKey));
        SigningCredentials signingCredentials = new(
            signingKey,
            SecurityAlgorithms.HmacSha256);
        JwtSecurityToken token = new(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        string accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        long expiresIn = (long)(expiresAt - issuedAt).TotalSeconds;
        return Task.FromResult(new TokenResult(accessToken, expiresIn));
    }
}
