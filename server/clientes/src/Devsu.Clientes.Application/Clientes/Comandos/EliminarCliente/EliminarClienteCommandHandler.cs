using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Domain.Excepciones;
using Devsu.Clientes.Domain.Repositorios;
using Devsu.Contracts.Eventos;
using MediatR;

namespace Devsu.Clientes.Application.Clientes.Comandos.EliminarCliente;

public sealed class EliminarClienteCommandHandler : IRequestHandler<EliminarClienteCommand>
{
    private readonly IRepositorioCliente _clientes;
    private readonly IPublicadorEventos _eventos;
    private readonly IProveedorFechaHora _reloj;
    private readonly IUnitOfWork _unidadTrabajo;

    public EliminarClienteCommandHandler(
        IRepositorioCliente clientes,
        IPublicadorEventos eventos,
        IProveedorFechaHora reloj,
        IUnitOfWork unidadTrabajo)
    {
        _clientes = clientes;
        _eventos = eventos;
        _reloj = reloj;
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task Handle(EliminarClienteCommand peticion, CancellationToken cancelacion)
    {
        var cliente = await _clientes.ObtenerParaEdicionAsync(peticion.Id, cancelacion)
            ?? throw RecursoNoEncontradoException.Cliente(peticion.Id);

        if (peticion.Definitivo)
        {
            _clientes.Eliminar(cliente);

            await _eventos.EncolarAsync(
                new ClienteEliminado(Guid.NewGuid(), _reloj.AhoraUtc, cliente.Id),
                cancelacion);
        }
        else
        {
            cliente.CambiarEstado(false);

            await _eventos.EncolarAsync(
                new ClienteEstadoCambiado(
                    Guid.NewGuid(),
                    _reloj.AhoraUtc,
                    cliente.Id,
                    cliente.IdEstadoCliente,
                    cliente.Activo),
                cancelacion);
        }

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
    }
}
