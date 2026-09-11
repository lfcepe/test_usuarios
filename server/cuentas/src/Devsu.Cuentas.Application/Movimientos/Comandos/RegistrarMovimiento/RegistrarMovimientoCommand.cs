using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Movimientos.Contratos;
using MediatR;

namespace Devsu.Cuentas.Application.Movimientos.Comandos.RegistrarMovimiento;

/// <summary>Registro de un movimiento sobre una cuenta (F2 y F3 del enunciado).</summary>
public sealed record RegistrarMovimientoCommand(RegistrarMovimientoRequest Datos) : IRequest<MovimientoDto>;
