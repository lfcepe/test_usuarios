using Devsu.Clientes.Application.Dtos;
using Devsu.Clientes.Application.Mapeos;
using Devsu.Clientes.Domain.Excepciones;
using Devsu.Clientes.Domain.Repositorios;
using MediatR;

namespace Devsu.Clientes.Application.Personas.Consultas.ObtenerPersonaPorId;

public sealed class ObtenerPersonaPorIdQueryHandler : IRequestHandler<ObtenerPersonaPorIdQuery, PersonaDto>
{
    private readonly IRepositorioPersona _personas;

    public ObtenerPersonaPorIdQueryHandler(IRepositorioPersona personas)
    {
        _personas = personas;
    }

    public async Task<PersonaDto> Handle(ObtenerPersonaPorIdQuery peticion, CancellationToken cancelacion)
    {
        var persona = await _personas.ObtenerPorIdAsync(peticion.Id, cancelacion)
            ?? throw RecursoNoEncontradoException.Persona(peticion.Id);

        return persona.ToPersonaDto();
    }
}
