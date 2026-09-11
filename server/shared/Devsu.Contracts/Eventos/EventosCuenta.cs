namespace Devsu.Contracts.Eventos;

/// <summary>
/// Publicado por el microservicio de Cuentas al aperturar una cuenta.
/// Clientes lo consume para mantener el contador de "ResumenCuentasCliente".
/// </summary>
public sealed record CuentaAperturada(
    Guid IdMensaje,
    DateTime OcurridoEn,
    int IdCuenta,
    string NumeroCuenta,
    int IdCliente,
    decimal SaldoInicial) : IEventoIntegracion;

/// <summary>
/// Publicado al activar o desactivar una cuenta.
/// </summary>
public sealed record CuentaEstadoCambiado(
    Guid IdMensaje,
    DateTime OcurridoEn,
    int IdCuenta,
    string NumeroCuenta,
    int IdCliente,
    bool Activa) : IEventoIntegracion;
