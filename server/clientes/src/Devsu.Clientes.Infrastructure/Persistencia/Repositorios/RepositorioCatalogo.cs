using Devsu.Clientes.Domain.Entidades;
using Devsu.Clientes.Domain.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Clientes.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioCatalogo : IRepositorioCatalogo
{
    private readonly ClientesDbContext _contexto;

    public RepositorioCatalogo(ClientesDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<IReadOnlyList<Catalogo>> ObtenerPorRaizAsync(int idRaiz, CancellationToken cancelacion) =>
        await _contexto.Catalogos
            .AsNoTracking()
            .Where(catalogo => catalogo.IdRaiz == idRaiz)
            .OrderBy(catalogo => catalogo.Id)
            .ToListAsync(cancelacion);

    public async Task<IReadOnlyList<Catalogo>> ObtenerRaicesAsync(CancellationToken cancelacion) =>
        await _contexto.Catalogos
            .AsNoTracking()
            .Where(catalogo => catalogo.IdRaiz == null)
            .OrderBy(catalogo => catalogo.Id)
            .ToListAsync(cancelacion);

    public Task<bool> PerteneceARaizAsync(int idCatalogo, int idRaiz, CancellationToken cancelacion) =>
        _contexto.Catalogos
            .AsNoTracking()
            .AnyAsync(catalogo => catalogo.Id == idCatalogo && catalogo.IdRaiz == idRaiz, cancelacion);
}
