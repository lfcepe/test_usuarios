using Devsu.Cuentas.Domain.Entidades;
using Devsu.Cuentas.Domain.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Cuentas.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioOutbox : IRepositorioOutbox
{
    private readonly CuentasDbContext _contexto;

    public RepositorioOutbox(CuentasDbContext contexto)
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
