using System.Text.Json;
using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Domain.Repositorios;
using Devsu.Contracts.Eventos;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Devsu.Cuentas.Infrastructure.Mensajeria;

/// <summary>
/// Vacia periodicamente la tabla "OutboxMensajes" hacia RabbitMQ.
/// </summary>
/// <remarks>
/// Es la segunda mitad del patron Transactional Outbox. Si el broker no responde,
/// el mensaje se queda en la tabla con el contador de intentos incrementado y se
/// reintenta en el siguiente ciclo; nada se pierde y el servicio sigue atendiendo
/// peticiones HTTP con normalidad.
/// </remarks>
public sealed class PublicadorOutboxHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _fabricaScopes;
    private readonly OpcionesMensajeria _opciones;
    private readonly ILogger<PublicadorOutboxHostedService> _log;

    public PublicadorOutboxHostedService(
        IServiceScopeFactory fabricaScopes,
        IOptions<OpcionesMensajeria> opciones,
        ILogger<PublicadorOutboxHostedService> log)
    {
        _fabricaScopes = fabricaScopes;
        _opciones = opciones.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken cancelacion)
    {
        if (!_opciones.Habilitado)
        {
            _log.LogInformation("Mensajeria desactivada: el publicador del outbox no se inicia");
            return;
        }

        var intervalo = TimeSpan.FromSeconds(Math.Max(1, _opciones.IntervaloOutboxSegundos));

        // Se espera un ciclo antes del primer intento para dar tiempo a que la
        // base de datos y el broker terminen de arrancar dentro de Docker.
        await EsperarAsync(intervalo, cancelacion);

        while (!cancelacion.IsCancellationRequested)
        {
            try
            {
                await ProcesarPendientesAsync(cancelacion);
            }
            catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
            {
                break;
            }
            catch (Exception excepcion)
            {
                // Un fallo aqui no puede tumbar el host: el outbox se reintenta solo.
                _log.LogError(excepcion, "Fallo el ciclo de publicacion del outbox");
            }

            await EsperarAsync(intervalo, cancelacion);
        }
    }

    private async Task ProcesarPendientesAsync(CancellationToken cancelacion)
    {
        using var scope = _fabricaScopes.CreateScope();

        var outbox = scope.ServiceProvider.GetRequiredService<IRepositorioOutbox>();
        var publicador = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var unidadTrabajo = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var reloj = scope.ServiceProvider.GetRequiredService<IProveedorFechaHora>();

        var pendientes = await outbox.ObtenerPendientesAsync(_opciones.LoteOutbox, cancelacion);

        if (pendientes.Count == 0)
        {
            return;
        }

        foreach (var mensaje in pendientes)
        {
            try
            {
                var tipo = ResolverTipo(mensaje.TipoMensaje);

                if (tipo is null)
                {
                    // Un tipo desconocido no se puede publicar nunca. Se marca como
                    // procesado con el error anotado para que no bloquee la cola.
                    mensaje.RegistrarFallo($"Tipo de evento desconocido: {mensaje.TipoMensaje}");
                    mensaje.MarcarProcesado(reloj.AhoraUtc);
                    _log.LogError("Evento {Id} descartado por tipo desconocido", mensaje.Id);
                    continue;
                }

                var evento = JsonSerializer.Deserialize(
                    mensaje.Contenido,
                    tipo,
                    PublicadorEventosOutbox.OpcionesJson);

                if (evento is null)
                {
                    mensaje.RegistrarFallo("El contenido del evento no se pudo deserializar");
                    continue;
                }

                await publicador.Publish(evento, tipo, cancelacion);
                mensaje.MarcarProcesado(reloj.AhoraUtc);

                _log.LogInformation("Evento {Tipo} publicado ({Id})", mensaje.TipoMensaje, mensaje.Id);
            }
            catch (Exception excepcion)
            {
                mensaje.RegistrarFallo(excepcion.Message);
                _log.LogWarning(
                    excepcion,
                    "No se pudo publicar el evento {Id}, intento numero {Intentos}",
                    mensaje.Id,
                    mensaje.Intentos);
            }
        }

        await unidadTrabajo.GuardarCambiosAsync(cancelacion);
    }

    /// <summary>Resuelve el tipo CLR a partir del nombre guardado en la tabla.</summary>
    private static Type? ResolverTipo(string tipoMensaje) =>
        typeof(IEventoIntegracion).Assembly.GetType(tipoMensaje, throwOnError: false);

    private static async Task EsperarAsync(TimeSpan intervalo, CancellationToken cancelacion)
    {
        try
        {
            await Task.Delay(intervalo, cancelacion);
        }
        catch (TaskCanceledException)
        {
            // Apagado normal del servicio.
        }
    }
}
