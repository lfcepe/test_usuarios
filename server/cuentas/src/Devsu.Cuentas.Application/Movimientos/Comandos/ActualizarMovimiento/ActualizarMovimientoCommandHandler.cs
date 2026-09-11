using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Mapeos;
using Devsu.Cuentas.Domain.Excepciones;
using Devsu.Cuentas.Domain.Repositorios;
using MediatR;

namespace Devsu.Cuentas.Application.Movimientos.Comandos.ActualizarMovimiento;

/// <summary>
/// Corrige el valor de un movimiento y deja la cadena de saldos coherente.
/// </summary>
/// <remarks>
/// Se carga la cuenta con todos sus movimientos porque cambiar uno invalida el
/// saldo de todos los posteriores. Es una operacion cara a proposito: corregir un
/// asiento contable no deberia ser barato ni frecuente.
/// </remarks>
public sealed class ActualizarMovimientoCommandHandler
    : IRequestHandler<ActualizarMovimientoCommand, MovimientoDto>
{
    private readonly IRepositorioMovimiento _movimientos;
    private readonly IRepositorioCuenta _cuentas;
    private readonly IUnitOfWork _unidadTrabajo;

    public ActualizarMovimientoCommandHandler(
        IRepositorioMovimiento movimientos,
        IRepositorioCuenta cuentas,
        IUnitOfWork unidadTrabajo)
    {
        _movimientos = movimientos;
        _cuentas = cuentas;
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<MovimientoDto> Handle(
        ActualizarMovimientoCommand peticion,
        CancellationToken cancelacion)
    {
        var existente = await _movimientos.ObtenerPorIdAsync(peticion.Id, cancelacion)
            ?? throw RecursoNoEncontradoException.Movimiento(peticion.Id);

        return await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var cuenta = await _cuentas.ObtenerConMovimientosAsync(existente.IdCuentaPersona, ct)
                ?? throw RecursoNoEncontradoException.Cuenta(existente.IdCuentaPersona);

            var movimiento = cuenta.ActualizarMovimiento(
                peticion.Id,
                peticion.Datos.Valor,
                peticion.Datos.Descripcion);

            await _unidadTrabajo.GuardarCambiosAsync(ct);

            var persistido = await _movimientos.ObtenerPorIdAsync(movimiento.Id, ct);

            return persistido?.ToMovimientoDto() ?? movimiento.ToMovimientoDto(cuenta);
        }, cancelacion);
    }
}
