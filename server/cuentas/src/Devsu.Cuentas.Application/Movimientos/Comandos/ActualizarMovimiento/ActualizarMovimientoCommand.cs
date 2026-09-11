using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Movimientos.Contratos;
using MediatR;

namespace Devsu.Cuentas.Application.Movimientos.Comandos.ActualizarMovimiento;

/// <summary>Correccion del valor de un movimiento ya registrado.</summary>
public sealed record ActualizarMovimientoCommand(int Id, ActualizarMovimientoRequest Datos)
    : IRequest<MovimientoDto>;
