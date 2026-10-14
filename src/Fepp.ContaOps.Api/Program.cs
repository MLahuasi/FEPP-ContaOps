using Fepp.ContaOps.Api;
using Fepp.ContaOps.Api.Exceptions;
using Fepp.ContaOps.Api.Swagger;
using Fepp.ContaOps.Application;
using Fepp.ContaOps.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (SwaggerSettings.IsEnabled(builder.Configuration, app.Environment))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FEPP-ContaOps API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Punto de entrada público para las pruebas de integración con WebApplicationFactory.
/// </summary>
public partial class Program
{
}
