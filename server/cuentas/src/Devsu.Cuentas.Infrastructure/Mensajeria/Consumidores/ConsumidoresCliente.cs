using Devsu.Contracts.Eventos;
using Devsu.Cuentas.Application.Sincronizacion.Comandos;
using MassTransit;
using MediatR;

namespace Devsu.Cuentas.Infrastructure.Mensajeria.Consumidores;

/// <summary>Crea la referencia local cuando se da de alta un cliente.</summary>
public sealed class ConsumidorClienteCreado : IConsumer<ClienteCreado>
{
    private readonly ISender _mediador;

    public ConsumidorClienteCreado(ISender mediador)
    {
        _mediador = mediador;
    }

    public Task Consume(ConsumeContext<ClienteCreado> contexto)
    {
        var evento = contexto.Message;

        return _mediador.Send(
            new SincronizarClienteCommand(
                evento.IdMensaje,
                nameof(ClienteCreado),
                evento.IdCliente,
                evento.ClienteId,
                evento.NombreCompleto,
                evento.NumeroDocumento,
                evento.IdEstadoCliente,
                evento.Activo),
            contexto.CancellationToken);
    }
}

/// <summary>Refresca los datos replicados cuando cambian en el origen.</summary>
public sealed class ConsumidorClienteActualizado : IConsumer<ClienteActualizado>
{
    private readonly ISender _mediador;

    public ConsumidorClienteActualizado(ISender mediador)
    {
        _mediador = mediador;
    }

    public Task Consume(ConsumeContext<ClienteActualizado> contexto)
    {
        var evento = contexto.Message;

        return _mediador.Send(
            new SincronizarClienteCommand(
                evento.IdMensaje,
                nameof(ClienteActualizado),
                evento.IdCliente,
                evento.ClienteId,
                evento.NombreCompleto,
                evento.NumeroDocumento,
                evento.IdEstadoCliente,
                evento.Activo),
            contexto.CancellationToken);
    }
}

/// <summary>Propaga la activacion o baja logica del cliente.</summary>
public sealed class ConsumidorClienteEstadoCambiado : IConsumer<ClienteEstadoCambiado>
{
    private readonly ISender _mediador;

    public ConsumidorClienteEstadoCambiado(ISender mediador)
    {
        _mediador = mediador;
    }

    public Task Consume(ConsumeContext<ClienteEstadoCambiado> contexto)
    {
        var evento = contexto.Message;

        return _mediador.Send(
            new SincronizarClienteCommand(
                evento.IdMensaje,
                nameof(ClienteEstadoCambiado),
                evento.IdCliente,
                ClienteId: null,
                NombreCompleto: null,
                NumeroDocumento: null,
                evento.IdEstadoCliente,
                evento.Activo),
            contexto.CancellationToken);
    }
}

/// <summary>
/// Marca la referencia como inactiva cuando el cliente se borra en el origen.
/// </summary>
/// <remarks>
/// No se elimina la fila: hay cuentas y movimientos que la referencian y son
/// informacion contable que debe seguir siendo atribuible a un titular.
/// </remarks>
public sealed class ConsumidorClienteEliminado : IConsumer<ClienteEliminado>
{
    private readonly ISender _mediador;

    public ConsumidorClienteEliminado(ISender mediador)
    {
        _mediador = mediador;
    }

    public Task Consume(ConsumeContext<ClienteEliminado> contexto)
    {
        var evento = contexto.Message;

        return _mediador.Send(
            new SincronizarClienteCommand(
                evento.IdMensaje,
                nameof(ClienteEliminado),
                evento.IdCliente,
                ClienteId: null,
                NombreCompleto: null,
                NumeroDocumento: null,
                IdEstadoCliente: null,
                Activo: false,
                EsBaja: true),
            contexto.CancellationToken);
    }
}
