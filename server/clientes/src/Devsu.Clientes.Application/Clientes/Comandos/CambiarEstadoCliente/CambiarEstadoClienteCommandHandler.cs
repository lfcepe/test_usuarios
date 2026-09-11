using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Application.Dtos;
using Devsu.Clientes.Application.Mapeos;
using Devsu.Clientes.Domain.Excepciones;
using Devsu.Clientes.Domain.Repositorios;
using Devsu.Contracts.Eventos;
using MediatR;

namespace Devsu.Clientes.Application.Clientes.Comandos.CambiarEstadoCliente;

public sealed class CambiarEstadoClienteCommandHandler
    : IRequestHandler<CambiarEstadoClienteCommand, ClienteDto>
{
    private readonly IRepositorioCliente _clientes;
    private readonly IRepositorioResumenCuentas _resumenes;
    private readonly IPublicadorEventos _eventos;
    private readonly IProveedorFechaHora _reloj;
    private readonly IUnitOfWork _unidadTrabajo;

    public CambiarEstadoClienteCommandHandler(
        IRepositorioCliente clientes,
        IRepositorioResumenCuentas resumenes,
        IPublicadorEventos eventos,
        IProveedorFechaHora reloj,
        IUnitOfWork unidadTrabajo)
    {
        _clientes = clientes;
        _resumenes = resumenes;
        _eventos = eventos;
        _reloj = reloj;
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<ClienteDto> Handle(CambiarEstadoClienteCommand peticion, CancellationToken cancelacion)
    {
        var cliente = await _clientes.ObtenerParaEdicionAsync(peticion.Id, cancelacion)
            ?? throw RecursoNoEncontradoException.Cliente(peticion.Id);

        cliente.CambiarEstado(peticion.Estado);

        await _eventos.EncolarAsync(
            new ClienteEstadoCambiado(
                Guid.NewGuid(),
                _reloj.AhoraUtc,
                cliente.Id,
                cliente.IdEstadoCliente,
                cliente.Activo),
            cancelacion);

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        var totales = await _resumenes.ObtenerTotalesAsync(new[] { cliente.Id }, cancelacion);
        var actualizado = await _clientes.ObtenerPorIdAsync(cliente.Id, cancelacion) ?? cliente;

        return actualizado.ToClienteDto(totales.GetValueOrDefault(cliente.Id));
    }
}
