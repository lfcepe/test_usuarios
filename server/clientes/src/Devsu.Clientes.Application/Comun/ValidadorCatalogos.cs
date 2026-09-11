using Devsu.Clientes.Domain.Catalogos;
using Devsu.Clientes.Domain.Excepciones;
using Devsu.Clientes.Domain.Repositorios;

namespace Devsu.Clientes.Application.Comun;

/// <summary>
/// Comprueba que los identificadores de catalogo recibidos pertenezcan a la raiz
/// correcta.
/// </summary>
/// <remarks>
/// No se resuelve con FluentValidation porque necesita ir a la base de datos, y
/// tampoco en la entidad porque el dominio no consulta repositorios. Es una regla
/// de aplicacion y por eso vive aqui.
/// </remarks>
public sealed class ValidadorCatalogos
{
    private readonly IRepositorioCatalogo _catalogos;

    public ValidadorCatalogos(IRepositorioCatalogo catalogos)
    {
        _catalogos = catalogos;
    }

    public async Task ValidarDatosPersonalesAsync(
        int idTipoDocumento,
        int idGenero,
        CancellationToken cancelacion)
    {
        if (!await _catalogos.PerteneceARaizAsync(idTipoDocumento, CatalogoIds.Raiz.TipoDocumento, cancelacion))
        {
            throw new ReglaNegocioException(
                $"El identificador {idTipoDocumento} no corresponde a un tipo de documento valido.");
        }

        if (!await _catalogos.PerteneceARaizAsync(idGenero, CatalogoIds.Raiz.Genero, cancelacion))
        {
            throw new ReglaNegocioException(
                $"El identificador {idGenero} no corresponde a un genero valido.");
        }
    }
}
