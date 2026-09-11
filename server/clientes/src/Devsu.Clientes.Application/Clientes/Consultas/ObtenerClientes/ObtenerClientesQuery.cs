using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Application.Dtos;
using MediatR;

namespace Devsu.Clientes.Application.Clientes.Consultas.ObtenerClientes;

/// <summary>Listado paginado de clientes con filtro libre y por estado.</summary>
public sealed record ObtenerClientesQuery(
    string? Busqueda,
    bool? Estado,
    int? Pagina,
    int? Tamanio) : IRequest<ResultadoPaginado<ClienteDto>>;
