using Devsu.Cuentas.Domain.Entidades;
using Devsu.Cuentas.Domain.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Cuentas.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioCatalogo : IRepositorioCatalogo
{
    private readonly CuentasDbContext _contexto;

    public RepositorioCatalogo(CuentasDbContext contexto)
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
