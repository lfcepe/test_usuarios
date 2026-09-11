using Devsu.Clientes.Application.Clientes.Contratos;
using Devsu.Clientes.Application.Dtos;
using MediatR;

namespace Devsu.Clientes.Application.Clientes.Comandos.CrearCliente;

/// <summary>Alta de un cliente y de su persona asociada.</summary>
public sealed record CrearClienteCommand(CrearClienteRequest Datos) : IRequest<ClienteDto>;
