using Devsu.Cuentas.Domain.Entidades;

namespace Devsu.Cuentas.Domain.Repositorios;

/// <summary>Replica local del cliente, alimentada por eventos de integracion.</summary>
public interface IRepositorioClienteRef
{
    Task<ClienteRef?> ObtenerPorIdAsync(int idCliente, CancellationToken cancelacion);

    Task<ClienteRef?> ObtenerPorClienteIdAsync(string clienteId, CancellationToken cancelacion);

    Task AgregarAsync(ClienteRef cliente, CancellationToken cancelacion);
}
