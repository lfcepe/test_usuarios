namespace Devsu.Cuentas.Application.Cuentas.Contratos;

/// <summary>Cuerpo esperado por POST /api/cuentas.</summary>
public sealed record CrearCuentaRequest(
    int IdCliente,
    string NumeroCuenta,
    int IdTipoCuenta,
    decimal SaldoInicial,
    bool Estado = true);

/// <summary>
/// Cuerpo esperado por PUT /api/cuentas/{id}.
/// </summary>
/// <remarks>
/// No incluye el saldo ni el cliente a proposito. El saldo solo cambia mediante
/// movimientos, que es lo que deja rastro contable, y reasignar el titular de una
/// cuenta con historico no es una operacion legitima.
/// </remarks>
public sealed record ActualizarCuentaRequest(int IdTipoCuenta, bool Estado);

/// <summary>Cuerpo esperado por PATCH /api/cuentas/{id}/estado.</summary>
public sealed record CambiarEstadoRequest(bool Estado);
