using Devsu.Clientes.Application.Dtos;
using MediatR;

namespace Devsu.Clientes.Application.Clientes.Consultas.ObtenerClientePorDocumento;

/// <summary>
/// Busqueda por numero de documento.
/// </summary>
/// <remarks>
/// El tipo de documento es opcional: si no se indica se busca solo por numero,
/// que en la practica es como lo usa el operador de ventanilla.
/// </remarks>
public sealed record ObtenerClientePorDocumentoQuery(
    string NumeroDocumento,
    int? IdTipoDocumento) : IRequest<ClienteDto>;
