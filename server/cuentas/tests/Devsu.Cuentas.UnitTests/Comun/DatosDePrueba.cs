using Devsu.Cuentas.Domain.Catalogos;
using Devsu.Cuentas.Domain.Entidades;

namespace Devsu.Cuentas.UnitTests.Comun;

internal static class DatosDePrueba
{
    /// <summary>Fecha fija para que las pruebas no dependan del reloj del sistema.</summary>
    public static readonly DateTime Momento = new(2022, 2, 10, 9, 0, 0, DateTimeKind.Utc);

    public static Cuenta Cuenta(
        int idCliente = 1,
        string numeroCuenta = "478758",
        int idTipoCuenta = CatalogoIds.TipoCuenta.Ahorros,
        decimal saldoInicial = 2000m,
        bool activa = true,
        int id = 1) =>
        Domain.Entidades.Cuenta
            .Crear(idCliente, numeroCuenta, idTipoCuenta, saldoInicial, activa)
            .ConId(id);
}
