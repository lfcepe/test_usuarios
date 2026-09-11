using Devsu.Clientes.Application.Dtos;
using MediatR;

namespace Devsu.Clientes.Application.Clientes.Comandos.CambiarEstadoCliente;

/// <summary>Activa o desactiva un cliente sin tocar el resto de sus datos.</summary>
public sealed record CambiarEstadoClienteCommand(int Id, bool Estado) : IRequest<ClienteDto>;
