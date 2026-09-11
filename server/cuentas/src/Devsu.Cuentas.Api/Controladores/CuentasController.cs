using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Application.Cuentas.Comandos.ActualizarCuenta;
using Devsu.Cuentas.Application.Cuentas.Comandos.CambiarEstadoCuenta;
using Devsu.Cuentas.Application.Cuentas.Comandos.CrearCuenta;
using Devsu.Cuentas.Application.Cuentas.Consultas.ObtenerCuenta;
using Devsu.Cuentas.Application.Cuentas.Consultas.ObtenerCuentas;
using Devsu.Cuentas.Application.Cuentas.Contratos;
using Devsu.Cuentas.Application.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Devsu.Cuentas.Api.Controladores;

/// <summary>
/// Operaciones sobre cuentas.
/// </summary>
/// <remarks>
/// El enunciado pide CRU, no CRUD: no hay DELETE. Una cuenta con movimientos no se
/// borra, se desactiva con PATCH /api/cuentas/{id}/estado.
/// </remarks>
[ApiController]
[Route("api/cuentas")]
[Produces("application/json")]
public sealed class CuentasController : ControllerBase
{
    private readonly ISender _mediador;

    public CuentasController(ISender mediador)
    {
        _mediador = mediador;
    }

    /// <summary>Lista las cuentas de forma paginada.</summary>
    /// <param name="clienteId">Filtra por titular.</param>
    /// <param name="estado">true activas, false inactivas, vacio todas.</param>
    /// <param name="pagina">Numero de pagina, empieza en 1.</param>
    /// <param name="tamanio">Registros por pagina, maximo 100.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<CuentaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<CuentaDto>>> Listar(
        [FromQuery] int? clienteId,
        [FromQuery] bool? estado,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanio,
        CancellationToken cancelacion) =>
        Ok(await _mediador.Send(new ObtenerCuentasQuery(clienteId, estado, pagina, tamanio), cancelacion));

    /// <summary>Obtiene una cuenta por su identificador interno.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CuentaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CuentaDto>> Obtener(int id, CancellationToken cancelacion) =>
        Ok(await _mediador.Send(new ObtenerCuentaPorIdQuery(id), cancelacion));

    /// <summary>Obtiene una cuenta por su numero.</summary>
    [HttpGet("por-numero/{numeroCuenta}")]
    [ProducesResponseType(typeof(CuentaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CuentaDto>> ObtenerPorNumero(
        string numeroCuenta,
        CancellationToken cancelacion) =>
        Ok(await _mediador.Send(new ObtenerCuentaPorNumeroQuery(numeroCuenta), cancelacion));

    /// <summary>
    /// Apertura una cuenta para un cliente existente.
    /// </summary>
    /// <remarks>
    /// Si el cliente todavia no llego por el canal de eventos, el servicio lo
    /// consulta al microservicio de Clientes. Si tampoco responde, devuelve 409 con
    /// el codigo CLIENTE_NO_SINCRONIZADO en lugar de crear una cuenta huerfana.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(CuentaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CuentaDto>> Crear(
        [FromBody] CrearCuentaRequest peticion,
        CancellationToken cancelacion)
    {
        var creada = await _mediador.Send(new CrearCuentaCommand(peticion), cancelacion);

        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    /// <summary>Actualiza el tipo y el estado de una cuenta.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CuentaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CuentaDto>> Actualizar(
        int id,
        [FromBody] ActualizarCuentaRequest peticion,
        CancellationToken cancelacion) =>
        Ok(await _mediador.Send(new ActualizarCuentaCommand(id, peticion), cancelacion));

    /// <summary>Activa o desactiva una cuenta.</summary>
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(typeof(CuentaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CuentaDto>> CambiarEstado(
        int id,
        [FromBody] CambiarEstadoRequest peticion,
        CancellationToken cancelacion) =>
        Ok(await _mediador.Send(new CambiarEstadoCuentaCommand(id, peticion.Estado), cancelacion));
}
