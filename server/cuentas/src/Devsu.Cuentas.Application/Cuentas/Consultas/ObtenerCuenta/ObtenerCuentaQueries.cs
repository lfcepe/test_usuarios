using Devsu.Cuentas.Application.Dtos;
using MediatR;

namespace Devsu.Cuentas.Application.Cuentas.Consultas.ObtenerCuenta;

public sealed record ObtenerCuentaPorIdQuery(int Id) : IRequest<CuentaDto>;

public sealed record ObtenerCuentaPorNumeroQuery(string NumeroCuenta) : IRequest<CuentaDto>;
