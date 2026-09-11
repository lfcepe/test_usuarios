using Devsu.Cuentas.Domain.Entidades;

namespace Devsu.Cuentas.Domain.Repositorios;

/// <summary>Registro de eventos ya consumidos, para descartar reentregas.</summary>
public interface IRepositorioIdempotencia
{
    Task<bool> YaProcesadoAsync(Guid idMensaje, CancellationToken cancelacion);

    Task RegistrarAsync(MensajeProcesado mensaje, CancellationToken cancelacion);
}
