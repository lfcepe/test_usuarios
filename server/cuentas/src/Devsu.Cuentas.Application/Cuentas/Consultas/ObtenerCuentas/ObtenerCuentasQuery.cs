using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Application.Dtos;
using MediatR;

namespace Devsu.Cuentas.Application.Cuentas.Consultas.ObtenerCuentas;

public sealed record ObtenerCuentasQuery(
    int? ClienteId,
    bool? Estado,
    int? Pagina,
    int? Tamanio) : IRequest<ResultadoPaginado<CuentaDto>>;
