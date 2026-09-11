using Devsu.Cuentas.Domain.Entidades;
using Devsu.Cuentas.Domain.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Cuentas.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioIdempotencia : IRepositorioIdempotencia
{
    private readonly CuentasDbContext _contexto;

    public RepositorioIdempotencia(CuentasDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<bool> YaProcesadoAsync(Guid idMensaje, CancellationToken cancelacion) =>
        _contexto.MensajesProcesados
            .AsNoTracking()
            .AnyAsync(mensaje => mensaje.IdMensaje == idMensaje, cancelacion);

    public async Task RegistrarAsync(MensajeProcesado mensaje, CancellationToken cancelacion) =>
        await _contexto.MensajesProcesados.AddAsync(mensaje, cancelacion);
}
