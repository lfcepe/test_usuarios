using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Application.Dtos;
using MediatR;

namespace Devsu.Cuentas.Application.Movimientos.Consultas.ObtenerMovimientos;

public sealed record ObtenerMovimientosQuery(
    int? CuentaId,
    string? NumeroCuenta,
    DateTime? Desde,
    DateTime? Hasta,
    int? Pagina,
    int? Tamanio) : IRequest<ResultadoPaginado<MovimientoDto>>;

public sealed record ObtenerMovimientoPorIdQuery(int Id) : IRequest<MovimientoDto>;
