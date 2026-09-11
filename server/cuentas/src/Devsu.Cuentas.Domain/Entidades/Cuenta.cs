using Devsu.Cuentas.Domain.Catalogos;
using Devsu.Cuentas.Domain.Comun;
using Devsu.Cuentas.Domain.Excepciones;

namespace Devsu.Cuentas.Domain.Entidades;

/// <summary>
/// Cuenta bancaria. Se mapea a la tabla "CuentasPersona".
/// </summary>
/// <remarks>
/// Es la raiz del agregado: los movimientos solo nacen y cambian a traves de ella,
/// porque es la unica que conoce el saldo y puede decidir si la operacion es
/// legitima. Toda la funcionalidad F2 y F3 del enunciado vive aqui, no en el
/// manejador ni en el controlador.
/// </remarks>
public sealed class Cuenta : EntidadAuditable
{
    private readonly List<Movimiento> _movimientos = new();

    private Cuenta()
    {
    }

    public int IdCliente { get; private set; }

    public string NumeroCuenta { get; private set; } = null!;

    public int IdTipoCuenta { get; private set; }

    /// <summary>Saldo de apertura. No cambia nunca: es la base de la auditoria.</summary>
    public decimal SaldoInicial { get; private set; }

    /// <summary>Saldo vigente despues de todos los movimientos aplicados.</summary>
    public decimal SaldoDisponible { get; private set; }

    public int IdEstadoCuenta { get; private set; }

    public ClienteRef? Cliente { get; private set; }

    public Catalogo? TipoCuenta { get; private set; }

    public Catalogo? EstadoCuenta { get; private set; }

    public IReadOnlyList<Movimiento> Movimientos => _movimientos.AsReadOnly();

    public bool Activa => IdEstadoCuenta == CatalogoIds.EstadoCuenta.Activa;

    public static Cuenta Crear(
        int idCliente,
        string numeroCuenta,
        int idTipoCuenta,
        decimal saldoInicial,
        bool activa = true)
    {
        if (idCliente <= 0)
        {
            throw new ReglaNegocioException("La cuenta debe pertenecer a un cliente valido.");
        }

        var saldo = Guardas.ImporteNoNegativo(saldoInicial, nameof(SaldoInicial));

        return new Cuenta
        {
            IdCliente = idCliente,
            NumeroCuenta = Guardas.NumeroCuenta(numeroCuenta),
            IdTipoCuenta = Guardas.IdCatalogoValido(idTipoCuenta, nameof(IdTipoCuenta)),
            SaldoInicial = saldo,
            SaldoDisponible = saldo,
            IdEstadoCuenta = CatalogoIds.EstadoCuenta.Desde(activa),
        };
    }

    /// <summary>
    /// Registra un movimiento y actualiza el saldo disponible (F2 y F3).
    /// </summary>
    /// <param name="valor">Positivo deposita, negativo retira. Cero no es valido.</param>
    /// <param name="fecha">Momento del asiento.</param>
    /// <param name="idTipoMovimiento">
    /// Opcional. Si se omite se deduce del signo del valor; si se indica, debe ser
    /// coherente con ese signo.
    /// </param>
    /// <param name="descripcion">Glosa libre del movimiento.</param>
    public Movimiento RegistrarMovimiento(
        decimal valor,
        DateTime fecha,
        int? idTipoMovimiento = null,
        string? descripcion = null)
    {
        if (!Activa)
        {
            throw new CuentaInactivaException(NumeroCuenta);
        }

        var importe = Guardas.Importe(valor);

        if (importe == 0m)
        {
            throw new ReglaNegocioException("El valor del movimiento no puede ser cero.");
        }

        var tipo = ResolverTipoMovimiento(importe, idTipoMovimiento);

        var saldoResultante = Guardas.Importe(SaldoDisponible + importe);

        if (saldoResultante < 0m)
        {
            throw new SaldoNoDisponibleException(NumeroCuenta, SaldoDisponible, importe);
        }

        var movimiento = Movimiento.Crear(Id, fecha, tipo, importe, saldoResultante, descripcion);

        _movimientos.Add(movimiento);
        SaldoDisponible = saldoResultante;

        return movimiento;
    }

    /// <summary>
    /// Corrige un movimiento ya registrado y recalcula la cadena de saldos.
    /// </summary>
    /// <remarks>
    /// Cambiar el valor de un asiento invalida el saldo de ese movimiento y el de
    /// todos los posteriores, asi que hay que reconstruir la secuencia completa. Si
    /// en algun punto intermedio el saldo quedara negativo, la correccion se
    /// rechaza entera: no se admite un historico que en algun momento estuvo en
    /// descubierto.
    ///
    /// Requiere que el agregado se haya cargado con todos sus movimientos.
    /// </remarks>
    public Movimiento ActualizarMovimiento(int idMovimiento, decimal nuevoValor, string? descripcion)
    {
        var movimiento = _movimientos.FirstOrDefault(item => item.Id == idMovimiento)
            ?? throw RecursoNoEncontradoException.Movimiento(idMovimiento);

        var importe = Guardas.Importe(nuevoValor);

        if (importe == 0m)
        {
            throw new ReglaNegocioException("El valor del movimiento no puede ser cero.");
        }

        movimiento.EstablecerValor(importe);
        movimiento.EstablecerDescripcion(descripcion);

        RecalcularSaldos();

        return movimiento;
    }

    public void Actualizar(int idTipoCuenta, bool activa)
    {
        IdTipoCuenta = Guardas.IdCatalogoValido(idTipoCuenta, nameof(IdTipoCuenta));
        IdEstadoCuenta = CatalogoIds.EstadoCuenta.Desde(activa);
    }

    public void CambiarEstado(bool activa) => IdEstadoCuenta = CatalogoIds.EstadoCuenta.Desde(activa);

    /// <summary>Carga movimientos ya persistidos al reconstruir el agregado.</summary>
    /// <remarks>
    /// Lo usa el repositorio en escenarios donde EF no puebla la coleccion por si
    /// mismo, y las pruebas para montar un historico sin pasar por la base.
    /// </remarks>
    public void AdjuntarMovimientos(IEnumerable<Movimiento> movimientos)
    {
        foreach (var movimiento in movimientos)
        {
            if (_movimientos.All(item => !ReferenceEquals(item, movimiento)))
            {
                _movimientos.Add(movimiento);
            }
        }
    }

    private void RecalcularSaldos()
    {
        var saldo = SaldoInicial;

        foreach (var movimiento in _movimientos.OrderBy(item => item.Fecha).ThenBy(item => item.Id))
        {
            var saldoPrevio = saldo;
            saldo = Guardas.Importe(saldo + movimiento.Valor);

            if (saldo < 0m)
            {
                throw new SaldoNoDisponibleException(NumeroCuenta, saldoPrevio, movimiento.Valor);
            }

            movimiento.EstablecerSaldo(saldo);
        }

        SaldoDisponible = saldo;
    }

    private static int ResolverTipoMovimiento(decimal importe, int? idTipoMovimiento)
    {
        var deducido = CatalogoIds.TipoMovimiento.Desde(importe);

        if (idTipoMovimiento is null)
        {
            return deducido;
        }

        if (idTipoMovimiento != deducido)
        {
            throw new ReglaNegocioException(
                "El tipo de movimiento indicado no corresponde con el signo del valor. "
                + "Un valor negativo es un retiro y uno positivo un deposito.");
        }

        return idTipoMovimiento.Value;
    }
}
