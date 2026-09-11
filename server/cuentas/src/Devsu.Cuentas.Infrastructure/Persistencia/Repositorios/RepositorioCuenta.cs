using Devsu.Cuentas.Domain.Catalogos;
using Devsu.Cuentas.Domain.Entidades;
using Devsu.Cuentas.Domain.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Cuentas.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioCuenta : IRepositorioCuenta
{
    private readonly CuentasDbContext _contexto;

    public RepositorioCuenta(CuentasDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<Cuenta?> ObtenerPorIdAsync(int id, CancellationToken cancelacion) =>
        ConDescripciones().FirstOrDefaultAsync(cuenta => cuenta.Id == id, cancelacion);

    public Task<Cuenta?> ObtenerPorNumeroAsync(string numeroCuenta, CancellationToken cancelacion) =>
        ConDescripciones().FirstOrDefaultAsync(cuenta => cuenta.NumeroCuenta == numeroCuenta, cancelacion);

    public async Task<Cuenta?> ObtenerParaMovimientoAsync(int idCuenta, CancellationToken cancelacion)
    {
        await BloquearPorIdAsync(idCuenta, cancelacion);

        return await _contexto.Cuentas.FirstOrDefaultAsync(cuenta => cuenta.Id == idCuenta, cancelacion);
    }

    public async Task<Cuenta?> ObtenerParaMovimientoPorNumeroAsync(
        string numeroCuenta,
        CancellationToken cancelacion)
    {
        await BloquearPorNumeroAsync(numeroCuenta, cancelacion);

        return await _contexto.Cuentas
            .FirstOrDefaultAsync(cuenta => cuenta.NumeroCuenta == numeroCuenta, cancelacion);
    }

    public async Task<Cuenta?> ObtenerConMovimientosAsync(int idCuenta, CancellationToken cancelacion)
    {
        var cuenta = await _contexto.Cuentas
            .Include(item => item.Movimientos)
            .FirstOrDefaultAsync(item => item.Id == idCuenta, cancelacion);

        return cuenta;
    }

    public async Task<(IReadOnlyList<Cuenta> Items, int Total)> BuscarAsync(
        int? idCliente,
        bool? activa,
        int pagina,
        int tamanio,
        CancellationToken cancelacion)
    {
        var consulta = ConDescripciones();

        if (idCliente is > 0)
        {
            consulta = consulta.Where(cuenta => cuenta.IdCliente == idCliente);
        }

        if (activa.HasValue)
        {
            var idEstado = CatalogoIds.EstadoCuenta.Desde(activa.Value);
            consulta = consulta.Where(cuenta => cuenta.IdEstadoCuenta == idEstado);
        }

        var total = await consulta.CountAsync(cancelacion);

        var items = await consulta
            .OrderBy(cuenta => cuenta.NumeroCuenta)
            .Skip((pagina - 1) * tamanio)
            .Take(tamanio)
            .ToListAsync(cancelacion);

        return (items, total);
    }

    public async Task<IReadOnlyList<Cuenta>> ObtenerPorClienteAsync(
        int idCliente,
        CancellationToken cancelacion) =>
        await ConDescripciones()
            .Where(cuenta => cuenta.IdCliente == idCliente)
            .OrderBy(cuenta => cuenta.NumeroCuenta)
            .ToListAsync(cancelacion);

    public Task<bool> ExisteNumeroAsync(
        string numeroCuenta,
        int? idExcluido,
        CancellationToken cancelacion) =>
        _contexto.Cuentas
            .AsNoTracking()
            .AnyAsync(
                cuenta => cuenta.NumeroCuenta == numeroCuenta
                          && (idExcluido == null || cuenta.Id != idExcluido),
                cancelacion);

    public async Task AgregarAsync(Cuenta cuenta, CancellationToken cancelacion) =>
        await _contexto.Cuentas.AddAsync(cuenta, cancelacion);

    /// <summary>
    /// Bloquea la fila de la cuenta hasta el final de la transaccion.
    /// </summary>
    /// <remarks>
    /// Sin este bloqueo, dos retiros concurrentes sobre la misma cuenta pueden leer
    /// el mismo saldo disponible, comprobar los dos que hay fondos suficientes y
    /// dejar la cuenta en descubierto. SELECT ... FOR UPDATE serializa el acceso.
    ///
    /// Solo se aplica en PostgreSQL. SQLite, que es lo que usan las pruebas de
    /// integracion, no soporta esa sintaxis y ademas serializa las escrituras por
    /// si mismo, asi que ahi la consulta se omite sin perder garantias.
    /// </remarks>
    private async Task BloquearPorIdAsync(int idCuenta, CancellationToken cancelacion)
    {
        if (!_contexto.Database.IsNpgsql())
        {
            return;
        }

        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"CuentasPersona\" WHERE \"Id\" = {idCuenta} FOR UPDATE",
            cancelacion);
    }

    private async Task BloquearPorNumeroAsync(string numeroCuenta, CancellationToken cancelacion)
    {
        if (!_contexto.Database.IsNpgsql())
        {
            return;
        }

        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"CuentasPersona\" WHERE \"NumeroCuenta\" = {numeroCuenta} FOR UPDATE",
            cancelacion);
    }

    private IQueryable<Cuenta> ConDescripciones() =>
        _contexto.Cuentas
            .AsNoTracking()
            .Include(cuenta => cuenta.Cliente)
            .Include(cuenta => cuenta.TipoCuenta)
            .Include(cuenta => cuenta.EstadoCuenta);
}
