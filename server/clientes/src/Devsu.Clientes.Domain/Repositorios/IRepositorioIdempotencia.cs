using Devsu.Clientes.Domain.Entidades;

namespace Devsu.Clientes.Domain.Repositorios;

/// <summary>Registro de eventos ya consumidos, para descartar reentregas.</summary>
public interface IRepositorioIdempotencia
{
    Task<bool> YaProcesadoAsync(Guid idMensaje, CancellationToken cancelacion);

    Task RegistrarAsync(MensajeProcesado mensaje, CancellationToken cancelacion);
}
