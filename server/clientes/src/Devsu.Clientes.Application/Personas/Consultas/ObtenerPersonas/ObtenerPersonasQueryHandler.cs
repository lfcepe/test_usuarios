using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Application.Dtos;
using Devsu.Clientes.Application.Mapeos;
using Devsu.Clientes.Domain.Repositorios;
using MediatR;

namespace Devsu.Clientes.Application.Personas.Consultas.ObtenerPersonas;

public sealed class ObtenerPersonasQueryHandler
    : IRequestHandler<ObtenerPersonasQuery, ResultadoPaginado<PersonaDto>>
{
    private readonly IRepositorioPersona _personas;

    public ObtenerPersonasQueryHandler(IRepositorioPersona personas)
    {
        _personas = personas;
    }

    public async Task<ResultadoPaginado<PersonaDto>> Handle(
        ObtenerPersonasQuery peticion,
        CancellationToken cancelacion)
    {
        var (pagina, tamanio) = Paginacion.Normalizar(peticion.Pagina, peticion.Tamanio);

        var (items, total) = await _personas.BuscarAsync(peticion.Busqueda, pagina, tamanio, cancelacion);

        var dtos = items.Select(persona => persona.ToPersonaDto()).ToList();

        return new ResultadoPaginado<PersonaDto>(dtos, pagina, tamanio, total);
    }
}
