using Devsu.Cuentas.Domain.Catalogos;
using Devsu.Cuentas.Domain.Comun;

namespace Devsu.Cuentas.Domain.Entidades;

/// <summary>
/// Asiento sobre una cuenta. Se mapea a la tabla "Movimientos".
/// </summary>
/// <remarks>
/// No es una raiz de agregado: solo se crea y se modifica a traves de
/// <see cref="Cuenta"/>, que es quien conoce el saldo y puede garantizar la
/// coherencia. Por eso sus metodos de mutacion son internos.
/// </remarks>
public sealed class Movimiento : EntidadAuditable
{
    private Movimiento()
    {
    }

    public int IdCuentaPersona { get; private set; }

    public DateTime Fecha { get; private set; }

    public int IdTipoMovimiento { get; private set; }

    /// <summary>Positivo para deposito, negativo para retiro.</summary>
    public decimal Valor { get; private set; }

    /// <summary>Saldo de la cuenta despues de aplicar este movimiento.</summary>
    public decimal Saldo { get; private set; }

    public string? Descripcion { get; private set; }

    public int IdEstadoMovimiento { get; private set; }

    public Cuenta? Cuenta { get; private set; }

    public Catalogo? TipoMovimiento { get; private set; }

    public Catalogo? EstadoMovimiento { get; private set; }

    public bool EsRetiro => Valor < 0;

    public bool EsDeposito => Valor > 0;

    internal static Movimiento Crear(
        int idCuenta,
        DateTime fecha,
        int idTipoMovimiento,
        decimal valor,
        decimal saldoResultante,
        string? descripcion) => new()
        {
            IdCuentaPersona = idCuenta,
            Fecha = fecha,
            IdTipoMovimiento = Guardas.IdCatalogoValido(idTipoMovimiento, nameof(IdTipoMovimiento)),
            Valor = Guardas.Importe(valor),
            Saldo = Guardas.Importe(saldoResultante),
            Descripcion = Guardas.TextoOpcional(descripcion, nameof(Descripcion), 256),
            IdEstadoMovimiento = CatalogoIds.EstadoMovimiento.Aplicado,
        };

    internal void EstablecerSaldo(decimal saldoResultante) => Saldo = Guardas.Importe(saldoResultante);

    internal void EstablecerValor(decimal valor)
    {
        Valor = Guardas.Importe(valor);

        // El tipo se recalcula porque una correccion puede cambiar el signo y con
        // el la naturaleza del asiento: dejarlo como estaba falsearia el reporte.
        IdTipoMovimiento = CatalogoIds.TipoMovimiento.Desde(Valor);
    }

    internal void EstablecerDescripcion(string? descripcion) =>
        Descripcion = Guardas.TextoOpcional(descripcion, nameof(Descripcion), 256);
}
