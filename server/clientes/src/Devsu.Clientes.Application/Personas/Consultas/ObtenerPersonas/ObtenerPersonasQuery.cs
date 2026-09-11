using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Application.Dtos;
using MediatR;

namespace Devsu.Clientes.Application.Personas.Consultas.ObtenerPersonas;

public sealed record ObtenerPersonasQuery(
    string? Busqueda,
    int? Pagina,
    int? Tamanio) : IRequest<ResultadoPaginado<PersonaDto>>;
