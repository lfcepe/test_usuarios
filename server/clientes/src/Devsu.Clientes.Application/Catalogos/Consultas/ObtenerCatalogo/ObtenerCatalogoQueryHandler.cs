using Devsu.Clientes.Application.Dtos;
using Devsu.Clientes.Application.Mapeos;
using Devsu.Clientes.Domain.Catalogos;
using Devsu.Clientes.Domain.Excepciones;
using Devsu.Clientes.Domain.Repositorios;
using MediatR;

namespace Devsu.Clientes.Application.Catalogos.Consultas.ObtenerCatalogo;

public sealed class ObtenerCatalogoQueryHandler
    : IRequestHandler<ObtenerCatalogoQuery, IReadOnlyList<CatalogoDto>>
{
    private readonly IRepositorioCatalogo _catalogos;

    public ObtenerCatalogoQueryHandler(IRepositorioCatalogo catalogos)
    {
        _catalogos = catalogos;
    }

    public async Task<IReadOnlyList<CatalogoDto>> Handle(
        ObtenerCatalogoQuery peticion,
        CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(peticion.Raiz))
        {
            var raices = await _catalogos.ObtenerRaicesAsync(cancelacion);
            return raices.Select(catalogo => catalogo.ToCatalogoDto()).ToList();
        }

        if (!CatalogoIds.RaicesPorNombre.TryGetValue(peticion.Raiz.Trim(), out var idRaiz))
        {
            throw new ReglaNegocioException(
                $"El catalogo '{peticion.Raiz}' no existe. Consulte /api/catalogos para ver los disponibles.");
        }

        var items = await _catalogos.ObtenerPorRaizAsync(idRaiz, cancelacion);
        return items.Select(catalogo => catalogo.ToCatalogoDto()).ToList();
    }
}
