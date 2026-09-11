namespace Devsu.Cuentas.Application.Movimientos.Contratos;

/// <summary>
/// Cuerpo esperado por POST /api/movimientos.
/// </summary>
/// <remarks>
/// La cuenta se puede indicar por identificador o por numero. El numero es lo que
/// tiene a mano el cajero y evita una consulta previa desde el frontend.
/// El signo del valor determina el tipo: negativo retira, positivo deposita.
/// </remarks>
public sealed record RegistrarMovimientoRequest(
    int? IdCuenta,
    string? NumeroCuenta,
    decimal Valor,
    int? IdTipoMovimiento,
    string? Descripcion,
    DateTime? Fecha);

/// <summary>Cuerpo esperado por PUT /api/movimientos/{id}.</summary>
public sealed record ActualizarMovimientoRequest(decimal Valor, string? Descripcion);
