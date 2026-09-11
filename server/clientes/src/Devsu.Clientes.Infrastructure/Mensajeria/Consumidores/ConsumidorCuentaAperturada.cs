using Devsu.Clientes.Application.Sincronizacion.Comandos;
using Devsu.Contracts.Eventos;
using MassTransit;
using MediatR;

namespace Devsu.Clientes.Infrastructure.Mensajeria.Consumidores;

/// <summary>
/// Recibe la apertura de una cuenta y actualiza el contador local del cliente.
/// </summary>
/// <remarks>
/// El consumidor no toca la base directamente: traduce el evento a un comando de
/// la capa de aplicacion. Asi la logica se prueba sin broker y pasa por el mismo
/// pipeline de validacion y trazas que el resto de casos de uso.
/// </remarks>
public sealed class ConsumidorCuentaAperturada : IConsumer<CuentaAperturada>
{
    private readonly ISender _mediador;

    public ConsumidorCuentaAperturada(ISender mediador)
    {
        _mediador = mediador;
    }

    public Task Consume(ConsumeContext<CuentaAperturada> contexto) =>
        _mediador.Send(
            new SincronizarCuentaCommand(
                contexto.Message.IdMensaje,
                nameof(CuentaAperturada),
                contexto.Message.IdCliente,
                TipoCambioCuenta.Apertura),
            contexto.CancellationToken);
}
