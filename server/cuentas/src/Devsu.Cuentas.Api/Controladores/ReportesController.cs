using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Reportes.Consultas.GenerarReporte;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Devsu.Cuentas.Api.Controladores;

/// <summary>
/// Reporte de estado de cuenta (funcionalidad F4).
/// </summary>
/// <remarks>
/// Se exponen dos representaciones del mismo dato. La de la raiz reproduce
/// literalmente el JSON del enunciado, claves con espacios incluidas, para que la
/// validacion contra la prueba sea directa. La de /estado-cuenta agrupa por cuenta
/// y anade totales, que es lo que consume el frontend.
/// </remarks>
[ApiController]
[Route("api/reportes")]
[Produces("application/json")]
public sealed class ReportesController : ControllerBase
{
    private readonly ISender _mediador;

    public ReportesController(ISender mediador)
    {
        _mediador = mediador;
    }

    /// <summary>Reporte plano en el formato exacto del enunciado.</summary>
    /// <param name="fecha">
    /// Rango de fechas. Admite "2022-02-01,2022-02-28" y "01/02/2022-28/02/2022".
    /// Si se omite se toma el mes en curso.
    /// </param>
    /// <param name="cliente">Identificador numerico del cliente o su codigo CLI-000001.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReporteFilaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ReporteFilaDto>>> Generar(
        [FromQuery] string? fecha,
        [FromQuery] string? cliente,
        CancellationToken cancelacion)
    {
        var reporte = await _mediador.Send(
            new GenerarReporteQuery(cliente, fecha, null, null),
            cancelacion);

        return Ok(GenerarReporteQueryHandler.Aplanar(reporte));
    }

    /// <summary>Reporte agrupado por cuenta, con totales de debitos y creditos.</summary>
    /// <param name="clienteId">Identificador numerico del cliente o su codigo CLI-000001.</param>
    /// <param name="fechaInicio">Fecha inicial inclusiva.</param>
    /// <param name="fechaFin">Fecha final inclusiva.</param>
    [HttpGet("estado-cuenta")]
    [ProducesResponseType(typeof(ReporteEstadoCuentaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReporteEstadoCuentaDto>> EstadoCuenta(
        [FromQuery] string? clienteId,
        [FromQuery] DateOnly? fechaInicio,
        [FromQuery] DateOnly? fechaFin,
        CancellationToken cancelacion) =>
        Ok(await _mediador.Send(
            new GenerarReporteQuery(clienteId, null, fechaInicio, fechaFin),
            cancelacion));
}
