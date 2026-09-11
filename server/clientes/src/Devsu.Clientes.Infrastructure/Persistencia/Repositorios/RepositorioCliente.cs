using Devsu.Clientes.Domain.Catalogos;
using Devsu.Clientes.Domain.Entidades;
using Devsu.Clientes.Domain.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Clientes.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioCliente : IRepositorioCliente
{
    private readonly ClientesDbContext _contexto;

    public RepositorioCliente(ClientesDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken cancelacion) =>
        ConDescripciones().FirstOrDefaultAsync(cliente => cliente.Id == id, cancelacion);

    public Task<Cliente?> ObtenerPorClienteIdAsync(string clienteId, CancellationToken cancelacion) =>
        ConDescripciones().FirstOrDefaultAsync(cliente => cliente.ClienteId == clienteId, cancelacion);

    public Task<Cliente?> ObtenerParaEdicionAsync(int id, CancellationToken cancelacion) =>
        _contexto.Clientes.FirstOrDefaultAsync(cliente => cliente.Id == id, cancelacion);

    public Task<Cliente?> ObtenerPorDocumentoAsync(
        int idTipoDocumento,
        string numeroDocumento,
        CancellationToken cancelacion) =>
        ConDescripciones().FirstOrDefaultAsync(
            cliente => cliente.IdTipoDocumento == idTipoDocumento
                       && cliente.NumeroDocumento == numeroDocumento,
            cancelacion);

    public async Task<(IReadOnlyList<Cliente> Items, int Total)> BuscarAsync(
        string? busqueda,
        bool? activo,
        int pagina,
        int tamanio,
        CancellationToken cancelacion)
    {
        var consulta = ConDescripciones();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            // Se usa ToLower y Contains en lugar de ILike de PostgreSQL para que la
            // misma consulta funcione tambien con SQLite en las pruebas.
            var termino = busqueda.Trim().ToLower();

            consulta = consulta.Where(cliente =>
                cliente.PrimerNombre.ToLower().Contains(termino)
                || cliente.PrimerApellido.ToLower().Contains(termino)
                || (cliente.SegundoNombre != null && cliente.SegundoNombre.ToLower().Contains(termino))
                || (cliente.SegundoApellido != null && cliente.SegundoApellido.ToLower().Contains(termino))
                || cliente.NumeroDocumento.Contains(termino)
                || cliente.ClienteId.ToLower().Contains(termino));
        }

        if (activo.HasValue)
        {
            var idEstado = CatalogoIds.EstadoCliente.Desde(activo.Value);
            consulta = consulta.Where(cliente => cliente.IdEstadoCliente == idEstado);
        }

        var total = await consulta.CountAsync(cancelacion);

        var items = await consulta
            .OrderBy(cliente => cliente.PrimerApellido)
            .ThenBy(cliente => cliente.PrimerNombre)
            .ThenBy(cliente => cliente.Id)
            .Skip((pagina - 1) * tamanio)
            .Take(tamanio)
            .ToListAsync(cancelacion);

        return (items, total);
    }

    public Task<bool> ExisteDocumentoAsync(
        int idTipoDocumento,
        string numeroDocumento,
        int? idExcluido,
        CancellationToken cancelacion) =>
        _contexto.Personas
            .AsNoTracking()
            .AnyAsync(
                persona => persona.IdTipoDocumento == idTipoDocumento
                           && persona.NumeroDocumento == numeroDocumento
                           && (idExcluido == null || persona.Id != idExcluido),
                cancelacion);

    public async Task<int> ObtenerSiguienteConsecutivoAsync(CancellationToken cancelacion)
    {
        if (_contexto.Database.IsNpgsql())
        {
            // La secuencia es atomica: dos altas concurrentes obtienen valores
            // distintos sin necesidad de bloquear la tabla.
            var valor = await _contexto.Database
                .SqlQueryRaw<long>("SELECT nextval('seq_cliente_consecutivo') AS \"Value\"")
                .FirstAsync(cancelacion);

            return (int)valor;
        }

        // Respaldo para proveedores sin secuencias con nombre (SQLite en pruebas).
        // No es seguro frente a concurrencia, pero en pruebas no hay concurrencia.
        var maximo = await _contexto.Clientes
            .AsNoTracking()
            .Select(cliente => (int?)cliente.Id)
            .MaxAsync(cancelacion);

        return (maximo ?? 0) + 1;
    }

    public async Task<IReadOnlyList<Cliente>> ObtenerConContraseniaPendienteAsync(CancellationToken cancelacion) =>
        await _contexto.Clientes
            .Where(cliente => cliente.Contrasenia == Cliente.ContraseniaPendiente)
            .ToListAsync(cancelacion);

    public async Task AgregarAsync(Cliente cliente, CancellationToken cancelacion) =>
        await _contexto.Clientes.AddAsync(cliente, cancelacion);

    public void Eliminar(Cliente cliente) => _contexto.Clientes.Remove(cliente);

    /// <summary>
    /// Consulta base de solo lectura con los catalogos ya resueltos.
    /// </summary>
    /// <remarks>
    /// AsNoTracking porque son lecturas: evita que EF construya el grafo de
    /// seguimiento de cambios, que en listados es la mayor parte del coste.
    /// </remarks>
    private IQueryable<Cliente> ConDescripciones() =>
        _contexto.Clientes
            .AsNoTracking()
            .Include(cliente => cliente.TipoDocumento)
            .Include(cliente => cliente.Genero)
            .Include(cliente => cliente.EstadoCliente);
}
