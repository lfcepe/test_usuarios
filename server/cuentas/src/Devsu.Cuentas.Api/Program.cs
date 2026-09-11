using System.Text.Json.Serialization;
using Devsu.Cuentas.Api.Configuracion;
using Devsu.Cuentas.Api.Middleware;
using Devsu.Cuentas.Application;
using Devsu.Cuentas.Infrastructure;
using Devsu.Cuentas.Infrastructure.Persistencia;
using Serilog;

var constructor = WebApplication.CreateBuilder(args);

// Serilog se configura antes que nada para que los fallos de arranque tambien
// queden registrados con el mismo formato que el resto de la aplicacion.
constructor.Host.UseSerilog((contexto, configuracion) => configuracion
    .ReadFrom.Configuration(contexto.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

constructor.Services
    .AddControllers()
    .AddJsonOptions(opciones =>
    {
        opciones.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

constructor.Services.AgregarSwagger();
constructor.Services.AgregarAutenticacionFirebase(constructor.Configuration);
constructor.Services.AgregarApplication();
constructor.Services.AgregarInfrastructure(constructor.Configuration);

constructor.Services
    .AddHealthChecks()
    .AddDbContextCheck<CuentasDbContext>("base-datos", tags: new[] { "ready" });

// CORS abierto porque el frontend se sirve desde otro origen en desarrollo. En
// produccion se restringiria a los dominios reales mediante configuracion.
const string PoliticaCors = "devsu";
constructor.Services.AddCors(opciones =>
    opciones.AddPolicy(PoliticaCors, politica => politica
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()));

var aplicacion = constructor.Build();

// El middleware de excepciones va primero para poder capturar tambien lo que
// falle en los middlewares posteriores.
aplicacion.UseMiddleware<MiddlewareExcepciones>();

aplicacion.UseSerilogRequestLogging();

aplicacion.UseSwagger();
aplicacion.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Cuentas v1");
    opciones.DocumentTitle = "Devsu - Cuentas";
});

aplicacion.UseCors(PoliticaCors);

if (constructor.Configuration.GetValue<bool>("Firebase:Habilitado"))
{
    aplicacion.UseAuthentication();
    aplicacion.UseAuthorization();
}

aplicacion.MapControllers();

aplicacion.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
});

aplicacion.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = comprobacion => comprobacion.Tags.Contains("ready"),
});

await aplicacion.PrepararBaseDatosAsync();

aplicacion.Run();

/// <summary>
/// Declaracion parcial publica para que WebApplicationFactory pueda instanciar el
/// host desde el proyecto de pruebas de integracion.
/// </summary>
public partial class Program
{
}
