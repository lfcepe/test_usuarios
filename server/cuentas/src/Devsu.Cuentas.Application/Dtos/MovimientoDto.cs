namespace Devsu.Cuentas.Application.Dtos;

/// <summary>Representacion de un movimiento para el exterior del microservicio.</summary>
public sealed record MovimientoDto(
    int Id,
    int IdCuenta,
    string NumeroCuenta,
    DateTime Fecha,
    int IdTipoMovimiento,
    string? TipoMovimiento,
    decimal Valor,
    decimal Saldo,
    string? Descripcion,
    int IdEstadoMovimiento,
    string? EstadoDescripcion);
