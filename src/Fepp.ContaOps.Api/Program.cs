using Fepp.ContaOps.Api;
using Fepp.ContaOps.Api.Exceptions;
using Fepp.ContaOps.Api.Swagger;
using Fepp.ContaOps.Application;
using Fepp.ContaOps.Infrastructure;
using Fepp.ContaOps.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Configuración transversal y composición de las capas de la aplicación.
builder.Logging.AddJsonConsole();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// El seed valida la configuración segura y crea roles y administrador de forma idempotente.
using (IServiceScope scope = app.Services.CreateScope())
{
    IdentitySeeder seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
    await seeder.SeedAsync();
}

// El handler debe ejecutarse antes de los endpoints para uniformar excepciones no controladas.
app.UseExceptionHandler();

// Swagger se expone únicamente cuando la configuración y el ambiente lo permiten.
if (SwaggerSettings.IsEnabled(builder.Configuration, app.Environment))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FEPP-ContaOps API v1");
    });
}

// Redirige las solicitudes HTTP a HTTPS antes de procesar los endpoints.
app.UseHttpsRedirection();

// La autenticación y autorización se incorporarán al pipeline antes de proteger endpoints.
app.UseAuthorization();

// Descubre y registra los controllers de la API.
app.MapControllers();

app.Run();

/// <summary>
/// Punto de entrada público para las pruebas de integración con WebApplicationFactory.
/// </summary>
public partial class Program
{
}
