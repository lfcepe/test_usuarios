using MediatR;

namespace Devsu.Clientes.Application.Sincronizacion.Comandos;

/// <summary>
/// Aplica en el read model local un evento recibido del microservicio de Cuentas.
/// </summary>
/// <remarks>
/// El consumidor de MassTransit no toca la base directamente: traduce el evento a
/// este comando y lo envia por MediatR. Asi la logica de sincronizacion se prueba
/// sin levantar un broker y queda sujeta al mismo pipeline de validacion y log
/// que el resto de casos de uso.
/// </remarks>
public sealed record SincronizarCuentaCommand(
    Guid IdMensaje,
    string TipoMensaje,
    int IdCliente,
    TipoCambioCuenta Cambio) : IRequest;

public enum TipoCambioCuenta
{
    Apertura,
    Activacion,
    Desactivacion,
}
