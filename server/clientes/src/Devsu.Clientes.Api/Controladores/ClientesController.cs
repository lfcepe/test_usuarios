using Devsu.Clientes.Application.Clientes.Comandos.ActualizarCliente;
using Devsu.Clientes.Application.Clientes.Comandos.CambiarEstadoCliente;
using Devsu.Clientes.Application.Clientes.Comandos.CrearCliente;
using Devsu.Clientes.Application.Clientes.Comandos.EliminarCliente;
using Devsu.Clientes.Application.Clientes.Consultas.ObtenerClientePorDocumento;
using Devsu.Clientes.Application.Clientes.Consultas.ObtenerClientePorId;
using Devsu.Clientes.Application.Clientes.Consultas.ObtenerClientes;
using Devsu.Clientes.Application.Clientes.Contratos;
using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Application.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Devsu.Clientes.Api.Controladores;

/// <summary>Operaciones CRUD sobre clientes.</summary>
[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
public sealed class ClientesController : ControllerBase
{
    private readonly ISender _mediador;

    public ClientesController(ISender mediador)
    {
        _mediador = mediador;
    }

    /// <summary>Lista los clientes de forma paginada.</summary>
    /// <param name="busqueda">Texto libre sobre nombres, apellidos, documento o codigo de cliente.</param>
    /// <param name="estado">true para activos, false para inactivos, vacio para todos.</param>
    /// <param name="pagina">Numero de pagina, empieza en 1.</param>
    /// <param name="tamanio">Registros por pagina, maximo 100.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<ClienteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<ClienteDto>>> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] bool? estado,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanio,
        CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerClientesQuery(busqueda, estado, pagina, tamanio),
            cancelacion);

        return Ok(resultado);
    }

    /// <summary>Obtiene un cliente por su identificador interno.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> Obtener(int id, CancellationToken cancelacion) =>
        Ok(await _mediador.Send(new ObtenerClientePorIdQuery(id), cancelacion));

    /// <summary>Busca un cliente por su numero de documento.</summary>
    /// <param name="numeroDocumento">Numero de cedula, pasaporte o RUC.</param>
    /// <param name="idTipoDocumento">Tipo de documento. Si se omite se asume cedula.</param>
    [HttpGet("por-identificacion/{numeroDocumento}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> ObtenerPorDocumento(
        string numeroDocumento,
        [FromQuery] int? idTipoDocumento,
        CancellationToken cancelacion) =>
        Ok(await _mediador.Send(
            new ObtenerClientePorDocumentoQuery(numeroDocumento, idTipoDocumento),
            cancelacion));

    /// <summary>Crea un cliente junto con su persona asociada.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClienteDto>> Crear(
        [FromBody] CrearClienteRequest peticion,
        CancellationToken cancelacion)
    {
        var creado = await _mediador.Send(new CrearClienteCommand(peticion), cancelacion);

        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>Reemplaza los datos de un cliente existente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClienteDto>> Actualizar(
        int id,
        [FromBody] ActualizarClienteRequest peticion,
        CancellationToken cancelacion) =>
        Ok(await _mediador.Send(new ActualizarClienteCommand(id, peticion), cancelacion));

    /// <summary>Activa o desactiva un cliente.</summary>
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(typeof(ClienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> CambiarEstado(
        int id,
        [FromBody] CambiarEstadoRequest peticion,
        CancellationToken cancelacion) =>
        Ok(await _mediador.Send(new CambiarEstadoClienteCommand(id, peticion.Estado), cancelacion));

    /// <summary>Da de baja un cliente.</summary>
    /// <param name="id">Identificador del cliente.</param>
    /// <param name="definitivo">
    /// false (por defecto) realiza una baja logica; true borra la fila de la base.
    /// </param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(
        int id,
        [FromQuery] bool definitivo,
        CancellationToken cancelacion)
    {
        await _mediador.Send(new EliminarClienteCommand(id, definitivo), cancelacion);

        return NoContent();
    }
}
