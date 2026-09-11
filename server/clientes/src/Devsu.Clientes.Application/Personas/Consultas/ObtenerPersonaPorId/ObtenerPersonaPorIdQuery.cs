using Devsu.Clientes.Application.Dtos;
using MediatR;

namespace Devsu.Clientes.Application.Personas.Consultas.ObtenerPersonaPorId;

public sealed record ObtenerPersonaPorIdQuery(int Id) : IRequest<PersonaDto>;
