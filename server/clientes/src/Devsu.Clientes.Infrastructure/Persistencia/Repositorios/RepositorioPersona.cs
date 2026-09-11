using Devsu.Clientes.Domain.Entidades;
using Devsu.Clientes.Domain.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Clientes.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioPersona : IRepositorioPersona
{
    private readonly ClientesDbContext _contexto;

    public RepositorioPersona(ClientesDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<Persona?> ObtenerPorIdAsync(int id, CancellationToken cancelacion) =>
        ConDescripciones().FirstOrDefaultAsync(persona => persona.Id == id, cancelacion);

    public async Task<(IReadOnlyList<Persona> Items, int Total)> BuscarAsync(
        string? busqueda,
        int pagina,
        int tamanio,
        CancellationToken cancelacion)
    {
        var consulta = ConDescripciones();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToLower();

            consulta = consulta.Where(persona =>
                persona.PrimerNombre.ToLower().Contains(termino)
                || persona.PrimerApellido.ToLower().Contains(termino)
                || persona.NumeroDocumento.Contains(termino));
        }

        var total = await consulta.CountAsync(cancelacion);

        var items = await consulta
            .OrderBy(persona => persona.PrimerApellido)
            .ThenBy(persona => persona.PrimerNombre)
            .ThenBy(persona => persona.Id)
            .Skip((pagina - 1) * tamanio)
            .Take(tamanio)
            .ToListAsync(cancelacion);

        return (items, total);
    }

    private IQueryable<Persona> ConDescripciones() =>
        _contexto.Personas
            .AsNoTracking()
            .Include(persona => persona.TipoDocumento)
            .Include(persona => persona.Genero)
            .Include(persona => persona.EstadoPersona);
}
