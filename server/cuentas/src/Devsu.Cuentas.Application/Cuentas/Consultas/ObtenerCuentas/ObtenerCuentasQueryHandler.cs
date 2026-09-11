using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Mapeos;
using Devsu.Cuentas.Domain.Repositorios;
using MediatR;

namespace Devsu.Cuentas.Application.Cuentas.Consultas.ObtenerCuentas;

public sealed class ObtenerCuentasQueryHandler
    : IRequestHandler<ObtenerCuentasQuery, ResultadoPaginado<CuentaDto>>
{
    private readonly IRepositorioCuenta _cuentas;

    public ObtenerCuentasQueryHandler(IRepositorioCuenta cuentas)
    {
        _cuentas = cuentas;
    }

    public async Task<ResultadoPaginado<CuentaDto>> Handle(
        ObtenerCuentasQuery peticion,
        CancellationToken cancelacion)
    {
        var (pagina, tamanio) = Paginacion.Normalizar(peticion.Pagina, peticion.Tamanio);

        var (items, total) = await _cuentas.BuscarAsync(
            peticion.ClienteId,
            peticion.Estado,
            pagina,
            tamanio,
            cancelacion);

        var dtos = items.Select(cuenta => cuenta.ToCuentaDto()).ToList();

        return new ResultadoPaginado<CuentaDto>(dtos, pagina, tamanio, total);
    }
}
