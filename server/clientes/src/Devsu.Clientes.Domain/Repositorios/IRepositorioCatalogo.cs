using Devsu.Clientes.Domain.Entidades;

namespace Devsu.Clientes.Domain.Repositorios;

/// <summary>Lectura de la tabla de catalogos.</summary>
public interface IRepositorioCatalogo
{
    Task<IReadOnlyList<Catalogo>> ObtenerPorRaizAsync(int idRaiz, CancellationToken cancelacion);

    Task<IReadOnlyList<Catalogo>> ObtenerRaicesAsync(CancellationToken cancelacion);

    /// <summary>Verifica que un identificador pertenezca a la raiz indicada.</summary>
    Task<bool> PerteneceARaizAsync(int idCatalogo, int idRaiz, CancellationToken cancelacion);
}
