using Devsu.Cuentas.Domain.Entidades;
using Devsu.Cuentas.Domain.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Cuentas.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioMovimiento : IRepositorioMovimiento
{
    private readonly CuentasDbContext _contexto;

    public RepositorioMovimiento(CuentasDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<Movimiento?> ObtenerPorIdAsync(int id, CancellationToken cancelacion) =>
        ConDescripciones().FirstOrDefaultAsync(movimiento => movimiento.Id == id, cancelacion);

    public async Task<(IReadOnlyList<Movimiento> Items, int Total)> BuscarAsync(
        int? idCuenta,
        string? numeroCuenta,
        DateTime? desde,
        DateTime? hasta,
        int pagina,
        int tamanio,
        CancellationToken cancelacion)
    {
        var consulta = ConDescripciones();

        if (idCuenta is > 0)
        {
            consulta = consulta.Where(movimiento => movimiento.IdCuentaPersona == idCuenta);
        }

        if (!string.IsNullOrWhiteSpace(numeroCuenta))
        {
            var numero = numeroCuenta.Trim();
            consulta = consulta.Where(movimiento => movimiento.Cuenta!.NumeroCuenta == numero);
        }

        if (desde.HasValue)
        {
            consulta = consulta.Where(movimiento => movimiento.Fecha >= desde.Value);
        }

        if (hasta.HasValue)
        {
            consulta = consulta.Where(movimiento => movimiento.Fecha <= hasta.Value);
        }

        var total = await consulta.CountAsync(cancelacion);

        var items = await consulta
            .OrderByDescending(movimiento => movimiento.Fecha)
            .ThenByDescending(movimiento => movimiento.Id)
            .Skip((pagina - 1) * tamanio)
            .Take(tamanio)
            .ToListAsync(cancelacion);

        return (items, total);
    }

    public async Task<IReadOnlyList<Movimiento>> ObtenerParaReporteAsync(
        IReadOnlyCollection<int> idsCuenta,
        DateTime desde,
        DateTime hasta,
        CancellationToken cancelacion)
    {
        if (idsCuenta.Count == 0)
        {
            return Array.Empty<Movimiento>();
        }

        return await _contexto.Movimientos
            .AsNoTracking()
            .Include(movimiento => movimiento.TipoMovimiento)
            .Include(movimiento => movimiento.EstadoMovimiento)
            .Where(movimiento =>
                idsCuenta.Contains(movimiento.IdCuentaPersona)
                && movimiento.Fecha >= desde
                && movimiento.Fecha <= hasta)
            .OrderBy(movimiento => movimiento.Fecha)
            .ThenBy(movimiento => movimiento.Id)
            .ToListAsync(cancelacion);
    }

    private IQueryable<Movimiento> ConDescripciones() =>
        _contexto.Movimientos
            .AsNoTracking()
            .Include(movimiento => movimiento.Cuenta)
            .Include(movimiento => movimiento.TipoMovimiento)
            .Include(movimiento => movimiento.EstadoMovimiento);
}
