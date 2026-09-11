using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Application.Dtos;
using Devsu.Clientes.Application.Personas.Consultas.ObtenerPersonaPorId;
using Devsu.Clientes.Application.Personas.Consultas.ObtenerPersonas;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Devsu.Clientes.Api.Controladores;

/// <summary>
/// Consulta de personas.
/// </summary>
/// <remarks>
/// Es de solo lectura a proposito: una persona se da de alta a traves del alta de
/// cliente. Exponer un POST aqui permitiria crear personas huerfanas que ningun
/// caso de uso del enunciado necesita.
/// </remarks>
[ApiController]
[Route("api/personas")]
[Produces("application/json")]
public sealed class PersonasController : ControllerBase
{
    private readonly ISender _mediador;

    public PersonasController(ISender mediador)
    {
        _mediador = mediador;
    }

    /// <summary>Lista las personas registradas, sean clientes o no.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<PersonaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<PersonaDto>>> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanio,
        CancellationToken cancelacion) =>
        Ok(await _mediador.Send(new ObtenerPersonasQuery(busqueda, pagina, tamanio), cancelacion));

    /// <summary>Obtiene una persona por su identificador.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PersonaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonaDto>> Obtener(int id, CancellationToken cancelacion) =>
        Ok(await _mediador.Send(new ObtenerPersonaPorIdQuery(id), cancelacion));
}
