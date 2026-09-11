using Devsu.Cuentas.Application.Cuentas.Contratos;
using Devsu.Cuentas.Application.Dtos;
using MediatR;

namespace Devsu.Cuentas.Application.Cuentas.Comandos.ActualizarCuenta;

public sealed record ActualizarCuentaCommand(int Id, ActualizarCuentaRequest Datos) : IRequest<CuentaDto>;
