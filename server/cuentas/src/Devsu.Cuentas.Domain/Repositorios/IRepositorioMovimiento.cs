using Devsu.Cuentas.Domain.Entidades;

namespace Devsu.Cuentas.Domain.Repositorios;

/// <summary>Consulta de movimientos. Las altas y correcciones pasan por Cuenta.</summary>
public interface IRepositorioMovimiento
{
    Task<Movimiento?> ObtenerPorIdAsync(int id, CancellationToken cancelacion);

    Task<(IReadOnlyList<Movimiento> Items, int Total)> BuscarAsync(
        int? idCuenta,
        string? numeroCuenta,
        DateTime? desde,
        DateTime? hasta,
        int pagina,
        int tamanio,
        CancellationToken cancelacion);

    /// <summary>Movimientos de varias cuentas dentro de un rango, para el reporte.</summary>
    Task<IReadOnlyList<Movimiento>> ObtenerParaReporteAsync(
        IReadOnlyCollection<int> idsCuenta,
        DateTime desde,
        DateTime hasta,
        CancellationToken cancelacion);
}
