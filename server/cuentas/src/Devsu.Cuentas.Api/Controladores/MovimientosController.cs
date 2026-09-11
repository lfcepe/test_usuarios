using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Movimientos.Comandos.ActualizarMovimiento;
using Devsu.Cuentas.Application.Movimientos.Comandos.RegistrarMovimiento;
using Devsu.Cuentas.Application.Movimientos.Consultas.ObtenerMovimientos;
using Devsu.Cuentas.Application.Movimientos.Contratos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Devsu.Cuentas.Api.Controladores;

/// <summary>Registro y consulta de movimientos.</summary>
[ApiController]
[Route("api/movimientos")]
[Produces("application/json")]
public sealed class MovimientosController : ControllerBase
{
    private readonly ISender _mediador;

    public MovimientosController(ISender mediador)
    {
        _mediador = mediador;
    }

    /// <summary>Lista movimientos con filtros de cuenta y rango de fechas.</summary>
    /// <param name="cuentaId">Identificador interno de la cuenta.</param>
    /// <param name="numeroCuenta">Numero de cuenta. Alternativa a cuentaId.</param>
    /// <param name="desde">Fecha inicial inclusiva.</param>
    /// <param name="hasta">Fecha final inclusiva; si solo trae fecha se toma el dia completo.</param>
    /// <param name="pagina">Numero de pagina, empieza en 1.</param>
    /// <param name="tamanio">Registros por pagina, maximo 100.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<MovimientoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<MovimientoDto>>> Listar(
        [FromQuery] int? cuentaId,
        [FromQuery] string? numeroCuenta,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanio,
        CancellationToken cancelacion) =>
        Ok(await _mediador.Send(
            new ObtenerMovimientosQuery(cuentaId, numeroCuenta, desde, hasta, pagina, tamanio),
            cancelacion));

    /// <summary>Obtiene un movimiento por su identificador.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MovimientoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovimientoDto>> Obtener(int id, CancellationToken cancelacion) =>
        Ok(await _mediador.Send(new ObtenerMovimientoPorIdQuery(id), cancelacion));

    /// <summary>
    /// Registra un movimiento y actualiza el saldo disponible de la cuenta.
    /// </summary>
    /// <remarks>
    /// El signo del valor determina el tipo: negativo retira, positivo deposita.
    /// Si el retiro deja la cuenta en descubierto la respuesta es 400 con el titulo
    /// "Saldo no disponible" y el codigo SALDO_NO_DISPONIBLE.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(MovimientoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MovimientoDto>> Registrar(
        [FromBody] RegistrarMovimientoRequest peticion,
        CancellationToken cancelacion)
    {
        var creado = await _mediador.Send(new RegistrarMovimientoCommand(peticion), cancelacion);

        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>
    /// Corrige el valor de un movimiento ya registrado.
    /// </summary>
    /// <remarks>
    /// Recalcula el saldo de ese movimiento y el de todos los posteriores de la
    /// misma cuenta. Si la correccion dejara algun saldo intermedio en negativo, se
    /// rechaza entera.
    /// </remarks>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(MovimientoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovimientoDto>> Actualizar(
        int id,
        [FromBody] ActualizarMovimientoRequest peticion,
        CancellationToken cancelacion) =>
        Ok(await _mediador.Send(new ActualizarMovimientoCommand(id, peticion), cancelacion));
}
