using Devsu.Contracts.Eventos;

namespace Devsu.Cuentas.Application.Comun;

/// <summary>
/// Encola un evento de integracion. No lo envia al broker: lo deja en el outbox
/// para que se publique despues de confirmar la transaccion.
/// </summary>
public interface IPublicadorEventos
{
    Task EncolarAsync(IEventoIntegracion evento, CancellationToken cancelacion);
}
