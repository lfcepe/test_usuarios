using Devsu.Clientes.Application.Catalogos.Consultas.ObtenerCatalogo;
using Devsu.Clientes.Application.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Devsu.Clientes.Api.Controladores;

/// <summary>Catalogos que alimentan los selectores del frontend.</summary>
[ApiController]
[Route("api/catalogos")]
[Produces("application/json")]
public sealed class CatalogosController : ControllerBase
{
    private readonly ISender _mediador;

    public CatalogosController(ISender mediador)
    {
        _mediador = mediador;
    }

    /// <summary>Devuelve los items de un catalogo, o las raices si no se indica ninguno.</summary>
    /// <param name="raiz">
    /// Nombre de la raiz: TIPO_DOCUMENTO, GENERO, ESTADO_PERSONA, ESTADO_CLIENTE,
    /// TIPO_CUENTA, ESTADO_CUENTA, TIPO_MOVIMIENTO o ESTADO_MOVIMIENTO.
    /// </param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<CatalogoDto>>> Obtener(
        [FromQuery] string? raiz,
        CancellationToken cancelacion) =>
        Ok(await _mediador.Send(new ObtenerCatalogoQuery(raiz), cancelacion));
}
