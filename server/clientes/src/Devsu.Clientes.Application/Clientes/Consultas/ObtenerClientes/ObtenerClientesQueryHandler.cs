using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Application.Dtos;
using Devsu.Clientes.Application.Mapeos;
using Devsu.Clientes.Domain.Repositorios;
using MediatR;

namespace Devsu.Clientes.Application.Clientes.Consultas.ObtenerClientes;

public sealed class ObtenerClientesQueryHandler
    : IRequestHandler<ObtenerClientesQuery, ResultadoPaginado<ClienteDto>>
{
    private readonly IRepositorioCliente _clientes;
    private readonly IRepositorioResumenCuentas _resumenes;

    public ObtenerClientesQueryHandler(IRepositorioCliente clientes, IRepositorioResumenCuentas resumenes)
    {
        _clientes = clientes;
        _resumenes = resumenes;
    }

    public async Task<ResultadoPaginado<ClienteDto>> Handle(
        ObtenerClientesQuery peticion,
        CancellationToken cancelacion)
    {
        var (pagina, tamanio) = Paginacion.Normalizar(peticion.Pagina, peticion.Tamanio);

        var (items, total) = await _clientes.BuscarAsync(
            peticion.Busqueda,
            peticion.Estado,
            pagina,
            tamanio,
            cancelacion);

        if (items.Count == 0)
        {
            return ResultadoPaginado<ClienteDto>.Vacio(pagina, tamanio);
        }

        // Una sola consulta para los totales de toda la pagina en lugar de una por
        // fila. Con veinte clientes la diferencia entre 1 y 21 consultas se nota.
        var totales = await _resumenes.ObtenerTotalesAsync(
            items.Select(cliente => cliente.Id).ToArray(),
            cancelacion);

        var dtos = items
            .Select(cliente => cliente.ToClienteDto(totales.GetValueOrDefault(cliente.Id)))
            .ToList();

        return new ResultadoPaginado<ClienteDto>(dtos, pagina, tamanio, total);
    }
}
