using Devsu.Cuentas.Domain.Entidades;
using Devsu.Cuentas.Domain.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Cuentas.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioClienteRef : IRepositorioClienteRef
{
    private readonly CuentasDbContext _contexto;

    public RepositorioClienteRef(CuentasDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<ClienteRef?> ObtenerPorIdAsync(int idCliente, CancellationToken cancelacion) =>
        _contexto.ClientesRef.FirstOrDefaultAsync(cliente => cliente.IdCliente == idCliente, cancelacion);

    public Task<ClienteRef?> ObtenerPorClienteIdAsync(string clienteId, CancellationToken cancelacion) =>
        _contexto.ClientesRef.FirstOrDefaultAsync(cliente => cliente.ClienteId == clienteId, cancelacion);

    public async Task AgregarAsync(ClienteRef cliente, CancellationToken cancelacion) =>
        await _contexto.ClientesRef.AddAsync(cliente, cancelacion);
}
