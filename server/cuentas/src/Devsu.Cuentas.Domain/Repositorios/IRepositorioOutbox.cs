using Devsu.Cuentas.Domain.Entidades;

namespace Devsu.Cuentas.Domain.Repositorios;

/// <summary>Cola persistente de eventos de integracion pendientes de publicar.</summary>
public interface IRepositorioOutbox
{
    /// <summary>Encola sin guardar: la escritura la confirma la unidad de trabajo.</summary>
    Task EncolarAsync(OutboxMensaje mensaje, CancellationToken cancelacion);

    Task<IReadOnlyList<OutboxMensaje>> ObtenerPendientesAsync(int maximo, CancellationToken cancelacion);
}
