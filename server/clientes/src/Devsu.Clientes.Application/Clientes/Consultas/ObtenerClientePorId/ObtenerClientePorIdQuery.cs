using Devsu.Clientes.Application.Dtos;
using MediatR;

namespace Devsu.Clientes.Application.Clientes.Consultas.ObtenerClientePorId;

public sealed record ObtenerClientePorIdQuery(int Id) : IRequest<ClienteDto>;
