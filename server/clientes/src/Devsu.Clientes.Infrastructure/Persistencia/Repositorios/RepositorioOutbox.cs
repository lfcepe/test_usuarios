using Devsu.Clientes.Domain.Entidades;
using Devsu.Clientes.Domain.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Clientes.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioOutbox : IRepositorioOutbox
{
    private readonly ClientesDbContext _contexto;

    public RepositorioOutbox(ClientesDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task EncolarAsync(OutboxMensaje mensaje, CancellationToken cancelacion) =>
        await _contexto.OutboxMensajes.AddAsync(mensaje, cancelacion);

    public async Task<IReadOnlyList<OutboxMensaje>> ObtenerPendientesAsync(
        int maximo,
        CancellationToken cancelacion) =>
        await _contexto.OutboxMensajes
            .Where(mensaje => mensaje.FechaProcesado == null)
            .OrderBy(mensaje => mensaje.FechaCreacion)
            .Take(maximo)
            .ToListAsync(cancelacion);
}
