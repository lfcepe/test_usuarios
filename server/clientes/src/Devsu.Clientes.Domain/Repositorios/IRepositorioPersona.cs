using Devsu.Clientes.Domain.Entidades;

namespace Devsu.Clientes.Domain.Repositorios;

/// <summary>Consulta de personas, incluidas las que todavia no son clientes.</summary>
public interface IRepositorioPersona
{
    Task<Persona?> ObtenerPorIdAsync(int id, CancellationToken cancelacion);

    Task<(IReadOnlyList<Persona> Items, int Total)> BuscarAsync(
        string? busqueda,
        int pagina,
        int tamanio,
        CancellationToken cancelacion);
}
