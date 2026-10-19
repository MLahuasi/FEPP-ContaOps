using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Results;
using Fepp.ContaOps.Domain.Users;
using Fepp.ContaOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fepp.ContaOps.Infrastructure.Identity;

/// <summary>
/// Implementa el ciclo de invitación y activación sobre ASP.NET Core Identity.
/// </summary>
public sealed class IdentityUserInvitationService : IUserInvitationService
{
    /// <summary>
    /// Nombre del proveedor dedicado para los tokens de invitación.
    /// </summary>
    public const string TokenProviderName = "UserInvitation";

    /// <summary>
    /// Propósito criptográfico separado de otros tokens de Identity.
    /// </summary>
    private const string TokenPurpose = "UserActivation";

    /// <summary>
    /// Administrador de usuarios de Identity.
    /// </summary>
    private readonly UserManager<ApplicationIdentityUser> _userManager;

    /// <summary>
    /// Contexto utilizado para agrupar los cambios de cada operación.
    /// </summary>
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Inicializa el servicio de invitaciones.
    /// </summary>
    /// <param name="userManager">Administrador de usuarios de Identity.</param>
    /// <param name="dbContext">Contexto de persistencia.</param>
    public IdentityUserInvitationService(
        UserManager<ApplicationIdentityUser> userManager,
        AppDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Result<UserInvitationResult>> RegisterPendingAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return Result<UserInvitationResult>.Failure(
                new ConflictError("A user with this email already exists."));
        }

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            ApplicationIdentityUser identityUser = new()
            {
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                Status = UserStatus.PendingActivation
            };

            EnsureSuccess(
                await _userManager.CreateAsync(identityUser),
                "The pending user could not be created.");
            EnsureSuccess(
                await _userManager.AddToRoleAsync(identityUser, IdentitySeeder.UserRole),
                "The pending user role could not be assigned.");

            string token = await _userManager.GenerateUserTokenAsync(
                identityUser,
                TokenProviderName,
                TokenPurpose);
            await transaction.CommitAsync(cancellationToken);

            return Result<UserInvitationResult>.Success(ToInvitation(identityUser, token));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Result<User>> ActivateAsync(
        UserId userId,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!userId.IsValid())
        {
            return Result<User>.Failure(InvalidActivationTokenError.Instance);
        }

        ApplicationIdentityUser? identityUser = await _userManager.FindByIdAsync(userId.Value);
        if (identityUser is null || identityUser.Status != UserStatus.PendingActivation)
        {
            return Result<User>.Failure(InvalidActivationTokenError.Instance);
        }

        bool tokenIsValid = await _userManager.VerifyUserTokenAsync(
            identityUser,
            TokenProviderName,
            TokenPurpose,
            token);
        if (!tokenIsValid)
        {
            return Result<User>.Failure(InvalidActivationTokenError.Instance);
        }

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            IdentityResult passwordResult = await _userManager.AddPasswordAsync(
                identityUser,
                newPassword);
            if (!passwordResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<User>.Failure(ToPasswordValidationError(passwordResult));
            }

            identityUser.EmailConfirmed = true;
            identityUser.Status = UserStatus.Active;
            EnsureSuccess(
                await _userManager.UpdateSecurityStampAsync(identityUser),
                "The account security state could not be updated.");
            await transaction.CommitAsync(cancellationToken);

            return Result<User>.Success(await ToDomainUserAsync(identityUser));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Result<UserInvitationResult>> ResendAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ApplicationIdentityUser? identityUser = await _userManager.FindByIdAsync(userId.Value);
        if (identityUser is null || identityUser.Status != UserStatus.PendingActivation)
        {
            return Result<UserInvitationResult>.Failure(
                new ResourceNotFoundError("pending user", userId.Value));
        }

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            EnsureSuccess(
                await _userManager.UpdateSecurityStampAsync(identityUser),
                "The invitation security state could not be updated.");
            string token = await _userManager.GenerateUserTokenAsync(
                identityUser,
                TokenProviderName,
                TokenPurpose);
            await transaction.CommitAsync(cancellationToken);

            return Result<UserInvitationResult>.Success(ToInvitation(identityUser, token));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Convierte un usuario técnico y su token al contrato de Application.
    /// </summary>
    /// <param name="identityUser">Usuario técnico de Identity.</param>
    /// <param name="token">Token dedicado generado para el usuario.</param>
    /// <returns>Resultado interno de invitación.</returns>
    private static UserInvitationResult ToInvitation(
        ApplicationIdentityUser identityUser,
        string token)
    {
        User user = new(
            new UserId(identityUser.Id),
            identityUser.Email ?? identityUser.UserName ?? string.Empty,
            [IdentitySeeder.UserRole],
            identityUser.Status);
        return new UserInvitationResult(user, token);
    }

    /// <summary>
    /// Convierte el usuario técnico activo al modelo de dominio con sus roles.
    /// </summary>
    /// <param name="identityUser">Usuario técnico de Identity.</param>
    /// <returns>Usuario de dominio con sus roles efectivos.</returns>
    private async Task<User> ToDomainUserAsync(ApplicationIdentityUser identityUser)
    {
        IList<string> roles = await _userManager.GetRolesAsync(identityUser);
        return new User(
            new UserId(identityUser.Id),
            identityUser.Email ?? identityUser.UserName ?? string.Empty,
            roles,
            identityUser.Status);
    }

    /// <summary>
    /// Traduce los códigos de política de contraseña de Identity a un error seguro.
    /// </summary>
    /// <param name="result">Resultado fallido de la operación de contraseña.</param>
    /// <returns>Error de validación agrupado para el campo de contraseña.</returns>
    private static ValidationError ToPasswordValidationError(IdentityResult result)
    {
        Dictionary<string, string[]> errors = new(StringComparer.Ordinal)
        {
            ["newPassword"] = result.Errors
                .Select(error => error.Code switch
                {
                    "PasswordTooShort" => "The password does not meet the minimum length.",
                    "PasswordRequiresUniqueChars" => "The password does not contain enough unique characters.",
                    "PasswordRequiresNonAlphanumeric" => "The password must contain a symbol.",
                    "PasswordRequiresDigit" => "The password must contain a digit.",
                    "PasswordRequiresLower" => "The password must contain a lowercase letter.",
                    "PasswordRequiresUpper" => "The password must contain an uppercase letter.",
                    _ => "The password does not meet the configured policy."
                })
                .Distinct(StringComparer.Ordinal)
                .ToArray()
        };

        return new ValidationError(errors);
    }

    /// <summary>
    /// Convierte un fallo de Identity en una excepción segura de infraestructura.
    /// </summary>
    /// <param name="result">Resultado devuelto por Identity.</param>
    /// <param name="message">Mensaje técnico sin datos sensibles.</param>
    private static void EnsureSuccess(IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        string errors = string.Join(", ", result.Errors.Select(error => error.Code));
        throw new InvalidOperationException($"{message} Identity errors: {errors}");
    }
}
