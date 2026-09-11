using Devsu.Cuentas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Cuentas.Api.Configuracion;

/// <summary>
/// Comprueba que la base de datos este lista antes de atender peticiones.
/// </summary>
/// <remarks>
/// No ejecuta migraciones: el esquema lo crea database/BaseDatos.sql, que es el
/// entregable del enunciado y la unica fuente de verdad del modelo. Lo que si hace
/// es esperar a que PostgreSQL acepte conexiones, porque dentro de Docker la API
/// arranca antes de que el motor termine de inicializarse.
/// </remarks>
public static class ArranqueBaseDatos
{
    private const int IntentosMaximos = 15;
    private static readonly TimeSpan EsperaEntreIntentos = TimeSpan.FromSeconds(3);

    public static async Task PrepararBaseDatosAsync(this WebApplication aplicacion)
    {
        using var alcance = aplicacion.Services.CreateScope();

        var log = alcance.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(ArranqueBaseDatos));
        var contexto = alcance.ServiceProvider.GetRequiredService<CuentasDbContext>();

        for (var intento = 1; intento <= IntentosMaximos; intento++)
        {
            try
            {
                if (await contexto.Database.CanConnectAsync())
                {
                    log.LogInformation("Conexion con la base de datos establecida");
                    await VerificarEsquemaAsync(contexto, log);
                    return;
                }
            }
            catch (Exception excepcion)
            {
                log.LogWarning(
                    "Intento {Intento} de {Maximo}: la base de datos aun no responde ({Mensaje})",
                    intento,
                    IntentosMaximos,
                    excepcion.Message);
            }

            await Task.Delay(EsperaEntreIntentos);
        }

        log.LogError(
            "No se pudo conectar con la base de datos tras {Maximo} intentos. "
            + "Verifique ConnectionStrings__Postgres y que BaseDatos.sql se haya ejecutado.",
            IntentosMaximos);
    }

    private static async Task VerificarEsquemaAsync(CuentasDbContext contexto, ILogger log)
    {
        try
        {
            await contexto.Catalogos.AsNoTracking().Take(1).ToListAsync();
        }
        catch (Exception excepcion)
        {
            log.LogError(
                excepcion,
                "La base responde pero el esquema no es el esperado. "
                + "Ejecute database/BaseDatos.sql antes de levantar el servicio.");
        }
    }
}
