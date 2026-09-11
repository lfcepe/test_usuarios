using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Mapeos;
using Devsu.Cuentas.Domain.Excepciones;
using Devsu.Cuentas.Domain.Repositorios;
using MediatR;

namespace Devsu.Cuentas.Application.Movimientos.Consultas.ObtenerMovimientos;

public sealed class ObtenerMovimientosQueryHandler
    : IRequestHandler<ObtenerMovimientosQuery, ResultadoPaginado<MovimientoDto>>
{
    private readonly IRepositorioMovimiento _movimientos;

    public ObtenerMovimientosQueryHandler(IRepositorioMovimiento movimientos)
    {
        _movimientos = movimientos;
    }

    public async Task<ResultadoPaginado<MovimientoDto>> Handle(
        ObtenerMovimientosQuery peticion,
        CancellationToken cancelacion)
    {
        var (pagina, tamanio) = Paginacion.Normalizar(peticion.Pagina, peticion.Tamanio);

        var (items, total) = await _movimientos.BuscarAsync(
            peticion.CuentaId,
            peticion.NumeroCuenta,
            peticion.Desde,
            // Si el usuario indica solo la fecha, se toma el dia completo. De lo
            // contrario "hasta=2022-02-28" excluiria todo lo ocurrido ese dia.
            AjustarFinDeDia(peticion.Hasta),
            pagina,
            tamanio,
            cancelacion);

        var dtos = items.Select(movimiento => movimiento.ToMovimientoDto()).ToList();

        return new ResultadoPaginado<MovimientoDto>(dtos, pagina, tamanio, total);
    }

    internal static DateTime? AjustarFinDeDia(DateTime? hasta) =>
        hasta is null
            ? null
            : hasta.Value.TimeOfDay == TimeSpan.Zero
                ? hasta.Value.Date.AddDays(1).AddTicks(-1)
                : hasta.Value;
}

public sealed class ObtenerMovimientoPorIdQueryHandler
    : IRequestHandler<ObtenerMovimientoPorIdQuery, MovimientoDto>
{
    private readonly IRepositorioMovimiento _movimientos;

    public ObtenerMovimientoPorIdQueryHandler(IRepositorioMovimiento movimientos)
    {
        _movimientos = movimientos;
    }

    public async Task<MovimientoDto> Handle(
        ObtenerMovimientoPorIdQuery peticion,
        CancellationToken cancelacion)
    {
        var movimiento = await _movimientos.ObtenerPorIdAsync(peticion.Id, cancelacion)
            ?? throw RecursoNoEncontradoException.Movimiento(peticion.Id);

        return movimiento.ToMovimientoDto();
    }
}
