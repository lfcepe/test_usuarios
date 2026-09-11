using Devsu.Cuentas.Domain.Catalogos;
using Devsu.Cuentas.Domain.Excepciones;
using Devsu.Cuentas.Domain.Repositorios;

namespace Devsu.Cuentas.Application.Comun;

/// <summary>Comprueba que los identificadores de catalogo pertenezcan a su raiz.</summary>
public sealed class ValidadorCatalogos
{
    private readonly IRepositorioCatalogo _catalogos;

    public ValidadorCatalogos(IRepositorioCatalogo catalogos)
    {
        _catalogos = catalogos;
    }

    public async Task ValidarTipoCuentaAsync(int idTipoCuenta, CancellationToken cancelacion)
    {
        if (!await _catalogos.PerteneceARaizAsync(idTipoCuenta, CatalogoIds.Raiz.TipoCuenta, cancelacion))
        {
            throw new ReglaNegocioException(
                $"El identificador {idTipoCuenta} no corresponde a un tipo de cuenta valido.");
        }
    }

    public async Task ValidarTipoMovimientoAsync(int? idTipoMovimiento, CancellationToken cancelacion)
    {
        if (idTipoMovimiento is null)
        {
            return;
        }

        if (!await _catalogos.PerteneceARaizAsync(
                idTipoMovimiento.Value, CatalogoIds.Raiz.TipoMovimiento, cancelacion))
        {
            throw new ReglaNegocioException(
                $"El identificador {idTipoMovimiento} no corresponde a un tipo de movimiento valido.");
        }
    }
}
