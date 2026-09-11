using Devsu.Clientes.Domain.Entidades;
using Devsu.Clientes.Domain.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Clientes.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioResumenCuentas : IRepositorioResumenCuentas
{
    private readonly ClientesDbContext _contexto;

    public RepositorioResumenCuentas(ClientesDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<ResumenCuentasCliente?> ObtenerPorClienteAsync(int idCliente, CancellationToken cancelacion) =>
        _contexto.ResumenCuentasClientes
            .FirstOrDefaultAsync(resumen => resumen.IdCliente == idCliente, cancelacion);

    public async Task<IReadOnlyDictionary<int, int>> ObtenerTotalesAsync(
        IReadOnlyCollection<int> idsCliente,
        CancellationToken cancelacion)
    {
        if (idsCliente.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        return await _contexto.ResumenCuentasClientes
            .AsNoTracking()
            .Where(resumen => idsCliente.Contains(resumen.IdCliente))
            .ToDictionaryAsync(
                resumen => resumen.IdCliente,
                resumen => resumen.TotalCuentas,
                cancelacion);
    }

    public async Task AgregarAsync(ResumenCuentasCliente resumen, CancellationToken cancelacion) =>
        await _contexto.ResumenCuentasClientes.AddAsync(resumen, cancelacion);
}
