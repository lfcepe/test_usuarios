using Devsu.Clientes.Domain.Entidades;

namespace Devsu.Clientes.Domain.Repositorios;

/// <summary>Read model del numero de cuentas por cliente.</summary>
public interface IRepositorioResumenCuentas
{
    Task<ResumenCuentasCliente?> ObtenerPorClienteAsync(int idCliente, CancellationToken cancelacion);

    Task<IReadOnlyDictionary<int, int>> ObtenerTotalesAsync(
        IReadOnlyCollection<int> idsCliente,
        CancellationToken cancelacion);

    Task AgregarAsync(ResumenCuentasCliente resumen, CancellationToken cancelacion);
}
