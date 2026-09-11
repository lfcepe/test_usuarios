using Devsu.Clientes.Application.Clientes.Contratos;
using Devsu.Clientes.Application.Dtos;
using MediatR;

namespace Devsu.Clientes.Application.Clientes.Comandos.ActualizarCliente;

/// <summary>Reemplazo completo de los datos de un cliente.</summary>
public sealed record ActualizarClienteCommand(int Id, ActualizarClienteRequest Datos) : IRequest<ClienteDto>;
