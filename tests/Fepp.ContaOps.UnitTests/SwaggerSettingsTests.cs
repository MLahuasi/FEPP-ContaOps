using Fepp.ContaOps.Api.Swagger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Fepp.ContaOps.UnitTests;

/// <summary>
/// Verifica la activación segura de Swagger por ambiente y configuración.
/// </summary>
public sealed class SwaggerSettingsTests
{
    /// <summary>
    /// Comprueba que producción queda deshabilitada sin configuración explícita.
    /// </summary>
    [Fact]
    public void IsEnabled_ProductionDefaultsToFalse()
    {
        ConfigurationManager configuration = new();

        Assert.False(SwaggerSettings.IsEnabled(configuration, new TestHostEnvironment("Production")));
    }

    /// <summary>
    /// Comprueba que la configuración explícita puede habilitar Swagger.
    /// </summary>
    [Fact]
    public void IsEnabled_ExplicitConfigurationWins()
    {
        ConfigurationManager configuration = new();
        configuration["Swagger:Enabled"] = "true";

        Assert.True(SwaggerSettings.IsEnabled(configuration, new TestHostEnvironment("Production")));
    }

    /// <summary>
    /// Ambiente mínimo para probar las decisiones de configuración.
    /// </summary>
    private sealed class TestHostEnvironment : IHostEnvironment
    {
        /// <summary>
        /// Inicializa el ambiente de prueba.
        /// </summary>
        /// <param name="environmentName">Nombre del ambiente.</param>
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
            ApplicationName = "Fepp.ContaOps.Api";
            ContentRootPath = AppContext.BaseDirectory;
            ContentRootFileProvider = new NullFileProvider();
        }

        /// <inheritdoc />
        public string EnvironmentName { get; set; }

        /// <inheritdoc />
        public string ApplicationName { get; set; }

        /// <inheritdoc />
        public string ContentRootPath { get; set; }

        /// <inheritdoc />
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
    }

    /// <summary>
    /// Proveedor de archivos vacío para el ambiente de prueba.
    /// </summary>
    private sealed class NullFileProvider : Microsoft.Extensions.FileProviders.IFileProvider
    {
        /// <inheritdoc />
        public Microsoft.Extensions.FileProviders.IDirectoryContents GetDirectoryContents(string subpath)
        {
            return Microsoft.Extensions.FileProviders.NotFoundDirectoryContents.Singleton;
        }

        /// <inheritdoc />
        public Microsoft.Extensions.FileProviders.IFileInfo GetFileInfo(string subpath)
        {
            return new Microsoft.Extensions.FileProviders.NotFoundFileInfo(subpath);
        }

        /// <inheritdoc />
        public Microsoft.Extensions.Primitives.IChangeToken Watch(string filter)
        {
            return new Microsoft.Extensions.Primitives.CancellationChangeToken(CancellationToken.None);
        }
    }
}
