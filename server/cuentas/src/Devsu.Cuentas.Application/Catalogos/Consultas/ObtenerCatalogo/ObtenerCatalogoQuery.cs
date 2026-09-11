using Devsu.Cuentas.Application.Dtos;
using MediatR;

namespace Devsu.Cuentas.Application.Catalogos.Consultas.ObtenerCatalogo;

/// <summary>
/// Items de un catalogo por el nombre de su raiz, por ejemplo TIPO_DOCUMENTO.
/// </summary>
/// <remarks>
/// Si no se indica raiz se devuelven las cabeceras, para que el frontend pueda
/// descubrir que catalogos existen sin tenerlos escritos a mano.
/// </remarks>
public sealed record ObtenerCatalogoQuery(string? Raiz) : IRequest<IReadOnlyList<CatalogoDto>>;
