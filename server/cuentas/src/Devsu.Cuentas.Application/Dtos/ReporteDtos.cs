using System.Text.Json.Serialization;

namespace Devsu.Cuentas.Application.Dtos;

/// <summary>
/// Fila del reporte de estado de cuenta con el formato literal del enunciado.
/// </summary>
/// <remarks>
/// Las claves llevan espacios ("Numero Cuenta", "Saldo Inicial") porque asi
/// aparecen en el ejemplo JSON de la prueba. No es la convencion del resto del
/// API, que usa camelCase; se respeta aqui unicamente para que el contrato
/// coincida caracter a caracter con lo solicitado.
/// </remarks>
public sealed record ReporteFilaDto
{
    [JsonPropertyName("Fecha")]
    public required string Fecha { get; init; }

    [JsonPropertyName("Cliente")]
    public required string Cliente { get; init; }

    [JsonPropertyName("Numero Cuenta")]
    public required string NumeroCuenta { get; init; }

    [JsonPropertyName("Tipo")]
    public required string Tipo { get; init; }

    [JsonPropertyName("Saldo Inicial")]
    public required decimal SaldoInicial { get; init; }

    [JsonPropertyName("Estado")]
    public required bool Estado { get; init; }

    [JsonPropertyName("Movimiento")]
    public required decimal Movimiento { get; init; }

    [JsonPropertyName("Saldo Disponible")]
    public required decimal SaldoDisponible { get; init; }
}

/// <summary>Cuenta con sus movimientos dentro del rango consultado.</summary>
public sealed record ReporteCuentaDto(
    int IdCuenta,
    string NumeroCuenta,
    string? TipoCuenta,
    decimal SaldoInicial,
    decimal SaldoDisponible,
    bool Estado,
    decimal TotalDebitos,
    decimal TotalCreditos,
    IReadOnlyList<MovimientoDto> Movimientos);

/// <summary>
/// Version enriquecida del reporte, agrupada por cuenta.
/// </summary>
/// <remarks>
/// Convive con <see cref="ReporteFilaDto"/>: el enunciado pide una lista plana y
/// esta forma agrupada es la que realmente consume el frontend, que necesita los
/// totales por cuenta sin tener que recalcularlos en el navegador.
/// </remarks>
public sealed record ReporteEstadoCuentaDto(
    int IdCliente,
    string? ClienteId,
    string Cliente,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    int TotalCuentas,
    int TotalMovimientos,
    IReadOnlyList<ReporteCuentaDto> Cuentas);
