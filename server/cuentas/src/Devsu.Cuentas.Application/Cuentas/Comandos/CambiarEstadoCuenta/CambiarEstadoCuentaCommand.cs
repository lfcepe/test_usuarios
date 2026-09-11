using Devsu.Cuentas.Application.Dtos;
using MediatR;

namespace Devsu.Cuentas.Application.Cuentas.Comandos.CambiarEstadoCuenta;

public sealed record CambiarEstadoCuentaCommand(int Id, bool Estado) : IRequest<CuentaDto>;
