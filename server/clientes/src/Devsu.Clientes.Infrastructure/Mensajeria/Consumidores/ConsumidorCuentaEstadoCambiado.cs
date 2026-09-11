using Devsu.Clientes.Application.Sincronizacion.Comandos;
using Devsu.Contracts.Eventos;
using MassTransit;
using MediatR;

namespace Devsu.Clientes.Infrastructure.Mensajeria.Consumidores;

/// <summary>
/// Mantiene el contador de cuentas activas cuando una cuenta se activa o desactiva.
/// </summary>
public sealed class ConsumidorCuentaEstadoCambiado : IConsumer<CuentaEstadoCambiado>
{
    private readonly ISender _mediador;

    public ConsumidorCuentaEstadoCambiado(ISender mediador)
    {
        _mediador = mediador;
    }

    public Task Consume(ConsumeContext<CuentaEstadoCambiado> contexto) =>
        _mediador.Send(
            new SincronizarCuentaCommand(
                contexto.Message.IdMensaje,
                nameof(CuentaEstadoCambiado),
                contexto.Message.IdCliente,
                contexto.Message.Activa ? TipoCambioCuenta.Activacion : TipoCambioCuenta.Desactivacion),
            contexto.CancellationToken);
}
