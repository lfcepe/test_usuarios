using Devsu.Cuentas.Domain.Entidades;

namespace Devsu.Cuentas.Domain.Repositorios;

/// <summary>Lectura de la tabla de catalogos.</summary>
public interface IRepositorioCatalogo
{
    Task<IReadOnlyList<Catalogo>> ObtenerPorRaizAsync(int idRaiz, CancellationToken cancelacion);

    Task<IReadOnlyList<Catalogo>> ObtenerRaicesAsync(CancellationToken cancelacion);

    Task<bool> PerteneceARaizAsync(int idCatalogo, int idRaiz, CancellationToken cancelacion);
}
