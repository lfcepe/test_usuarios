using Devsu.Cuentas.Application.Cuentas.Contratos;
using Devsu.Cuentas.Application.Dtos;
using MediatR;

namespace Devsu.Cuentas.Application.Cuentas.Comandos.CrearCuenta;

/// <summary>Apertura de una cuenta para un cliente existente.</summary>
public sealed record CrearCuentaCommand(CrearCuentaRequest Datos) : IRequest<CuentaDto>;
