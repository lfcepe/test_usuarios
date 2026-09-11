namespace Devsu.Cuentas.Application.Dtos;

/// <summary>Representacion de una cuenta para el exterior del microservicio.</summary>
public sealed record CuentaDto(
    int Id,
    int IdCliente,
    string? ClienteId,
    string? Cliente,
    string NumeroCuenta,
    int IdTipoCuenta,
    string? TipoCuenta,
    decimal SaldoInicial,
    decimal SaldoDisponible,
    bool Estado,
    int IdEstadoCuenta,
    string? EstadoDescripcion,
    DateTime FechaCreacion,
    DateTime? FechaModificacion);
