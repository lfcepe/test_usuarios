using Devsu.Cuentas.Domain.Entidades;

namespace Devsu.Cuentas.Domain.Repositorios;

/// <summary>Acceso a la raiz de agregado Cuenta.</summary>
public interface IRepositorioCuenta
{
    Task<Cuenta?> ObtenerPorIdAsync(int id, CancellationToken cancelacion);

    Task<Cuenta?> ObtenerPorNumeroAsync(string numeroCuenta, CancellationToken cancelacion);

    /// <summary>
    /// Carga la cuenta con seguimiento y bloqueo para registrar un movimiento.
    /// </summary>
    /// <remarks>
    /// En PostgreSQL aplica SELECT ... FOR UPDATE. Sin ese bloqueo, dos retiros
    /// simultaneos sobre la misma cuenta pueden leer el mismo saldo y dejarla en
    /// descubierto: los dos verian saldo suficiente antes de que el otro escriba.
    /// </remarks>
    Task<Cuenta?> ObtenerParaMovimientoAsync(int idCuenta, CancellationToken cancelacion);

    Task<Cuenta?> ObtenerParaMovimientoPorNumeroAsync(string numeroCuenta, CancellationToken cancelacion);

    /// <summary>Carga la cuenta con todos sus movimientos, para recalcular la cadena de saldos.</summary>
    Task<Cuenta?> ObtenerConMovimientosAsync(int idCuenta, CancellationToken cancelacion);

    Task<(IReadOnlyList<Cuenta> Items, int Total)> BuscarAsync(
        int? idCliente,
        bool? activa,
        int pagina,
        int tamanio,
        CancellationToken cancelacion);

    Task<IReadOnlyList<Cuenta>> ObtenerPorClienteAsync(int idCliente, CancellationToken cancelacion);

    Task<bool> ExisteNumeroAsync(string numeroCuenta, int? idExcluido, CancellationToken cancelacion);

    Task AgregarAsync(Cuenta cuenta, CancellationToken cancelacion);
}
