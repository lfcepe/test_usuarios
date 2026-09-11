using MediatR;

namespace Devsu.Cuentas.Application.Sincronizacion.Comandos;

/// <summary>
/// Aplica en "ClientesRef" un evento recibido del microservicio de Clientes.
/// </summary>
/// <remarks>
/// Los consumidores de MassTransit traducen el evento a este comando en lugar de
/// escribir en la base directamente. De esa forma la sincronizacion se puede
/// probar sin broker y pasa por el mismo pipeline que el resto de casos de uso.
/// </remarks>
public sealed record SincronizarClienteCommand(
    Guid IdMensaje,
    string TipoMensaje,
    int IdCliente,
    string? ClienteId,
    string? NombreCompleto,
    string? NumeroDocumento,
    int? IdEstadoCliente,
    bool? Activo,
    bool EsBaja = false) : IRequest;
