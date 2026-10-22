using Microsoft.Extensions.DependencyInjection;
using Fepp.ContaOps.Application.Users.ActivateUser;
using Fepp.ContaOps.Application.Users.CreateUserInvitation;
using Fepp.ContaOps.Application.Users.LoginUser;
using Fepp.ContaOps.Application.Users.ResendUserInvitation;
using Fepp.ContaOps.Application.Users.RefreshUser;
using Fepp.ContaOps.Application.Users.LogoutUser;

namespace Fepp.ContaOps.Application;

/// <summary>
/// Registra los servicios propios de la capa de aplicación.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Agrega los servicios de aplicación al contenedor de dependencias.
    /// </summary>
    /// <param name="services">Contenedor de servicios de la aplicación.</param>
    /// <returns>El mismo contenedor para permitir encadenar configuraciones.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginUser>();
        services.AddScoped<CreateUserInvitation>();
        services.AddScoped<ActivateUser>();
        services.AddScoped<ResendUserInvitation>();
        services.AddScoped<RefreshUser>();
        services.AddScoped<LogoutUser>();
        return services;
    }
}
