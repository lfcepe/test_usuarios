using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Domain.Entidades;

namespace Devsu.Cuentas.Application.Mapeos;

/// <summary>Conversion manual de entidades a DTOs.</summary>
public static class Mapeos
{
    public static CuentaDto ToCuentaDto(this Cuenta cuenta) => new(
        cuenta.Id,
        cuenta.IdCliente,
        cuenta.Cliente?.ClienteId,
        cuenta.Cliente?.NombreCompleto,
        cuenta.NumeroCuenta,
        cuenta.IdTipoCuenta,
        cuenta.TipoCuenta?.Item,
        cuenta.SaldoInicial,
        cuenta.SaldoDisponible,
        cuenta.Activa,
        cuenta.IdEstadoCuenta,
        cuenta.EstadoCuenta?.Item,
        cuenta.FechaCreacion,
        cuenta.FechaModificacion);

    public static MovimientoDto ToMovimientoDto(this Movimiento movimiento) => new(
        movimiento.Id,
        movimiento.IdCuentaPersona,
        movimiento.Cuenta?.NumeroCuenta ?? string.Empty,
        movimiento.Fecha,
        movimiento.IdTipoMovimiento,
        movimiento.TipoMovimiento?.Item,
        movimiento.Valor,
        movimiento.Saldo,
        movimiento.Descripcion,
        movimiento.IdEstadoMovimiento,
        movimiento.EstadoMovimiento?.Item);

    /// <summary>
    /// Variante para cuando el movimiento se acaba de crear y todavia no tiene
    /// cargadas sus navegaciones de catalogo.
    /// </summary>
    public static MovimientoDto ToMovimientoDto(this Movimiento movimiento, Cuenta cuenta) => new(
        movimiento.Id,
        cuenta.Id,
        cuenta.NumeroCuenta,
        movimiento.Fecha,
        movimiento.IdTipoMovimiento,
        movimiento.TipoMovimiento?.Item,
        movimiento.Valor,
        movimiento.Saldo,
        movimiento.Descripcion,
        movimiento.IdEstadoMovimiento,
        movimiento.EstadoMovimiento?.Item);

    public static CatalogoDto ToCatalogoDto(this Catalogo catalogo) => new(
        catalogo.Id,
        catalogo.DetalleCatalogo,
        catalogo.Item,
        catalogo.IdRaiz);
}
