using System.Text.Json;
using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Domain.Entidades;
using Devsu.Cuentas.Domain.Repositorios;
using Devsu.Contracts.Eventos;

namespace Devsu.Cuentas.Infrastructure.Mensajeria;

/// <summary>
/// Implementacion del patron Transactional Outbox: el evento no sale hacia el
/// broker, se guarda en la base dentro de la transaccion del caso de uso.
/// </summary>
/// <remarks>
/// Publicar directamente contra RabbitMQ desde el manejador tiene dos fallos
/// clasicos: si el broker esta caido se pierde el evento aunque el cambio si se
/// haya guardado, y si la transaccion termina en rollback el evento ya salio y
/// anuncia algo que nunca ocurrio. Guardarlo en la misma transaccion elimina las
/// dos ventanas.
/// </remarks>
public sealed class PublicadorEventosOutbox : IPublicadorEventos
{
    internal static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly IRepositorioOutbox _outbox;
    private readonly IProveedorFechaHora _reloj;

    public PublicadorEventosOutbox(IRepositorioOutbox outbox, IProveedorFechaHora reloj)
    {
        _outbox = outbox;
        _reloj = reloj;
    }

    public async Task EncolarAsync(IEventoIntegracion evento, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(evento);

        var tipo = evento.GetType();

        var mensaje = OutboxMensaje.Crear(
            evento.IdMensaje,
            tipo.FullName ?? tipo.Name,
            JsonSerializer.Serialize(evento, tipo, OpcionesJson),
            _reloj.AhoraUtc);

        await _outbox.EncolarAsync(mensaje, cancelacion);
    }
}
