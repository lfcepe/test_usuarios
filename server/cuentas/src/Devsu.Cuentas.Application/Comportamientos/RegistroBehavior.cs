using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Devsu.Cuentas.Application.Comportamientos;

/// <summary>
/// Registra el inicio, el fin y la duracion de cada caso de uso.
/// </summary>
/// <remarks>
/// El umbral de 500 ms marca las peticiones lentas para poder detectarlas en los
/// logs sin tener que activar trazas detalladas en produccion.
/// </remarks>
public sealed class RegistroBehavior<TPeticion, TRespuesta> : IPipelineBehavior<TPeticion, TRespuesta>
    where TPeticion : notnull
{
    private const int UmbralLentitudMs = 500;

    private readonly ILogger<RegistroBehavior<TPeticion, TRespuesta>> _log;

    public RegistroBehavior(ILogger<RegistroBehavior<TPeticion, TRespuesta>> log)
    {
        _log = log;
    }

    public async Task<TRespuesta> Handle(
        TPeticion peticion,
        RequestHandlerDelegate<TRespuesta> siguiente,
        CancellationToken cancelacion)
    {
        var nombre = typeof(TPeticion).Name;
        var cronometro = Stopwatch.StartNew();

        _log.LogInformation("Ejecutando {CasoDeUso}", nombre);

        try
        {
            var respuesta = await siguiente();
            cronometro.Stop();

            if (cronometro.ElapsedMilliseconds > UmbralLentitudMs)
            {
                _log.LogWarning(
                    "{CasoDeUso} tardo {Duracion} ms, por encima del umbral",
                    nombre,
                    cronometro.ElapsedMilliseconds);
            }
            else
            {
                _log.LogInformation("{CasoDeUso} completado en {Duracion} ms", nombre, cronometro.ElapsedMilliseconds);
            }

            return respuesta;
        }
        catch (Exception excepcion)
        {
            cronometro.Stop();
            _log.LogWarning(
                excepcion,
                "{CasoDeUso} fallo tras {Duracion} ms",
                nombre,
                cronometro.ElapsedMilliseconds);
            throw;
        }
    }
}
