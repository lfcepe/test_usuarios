using Devsu.Clientes.Domain.Entidades;
using Devsu.Clientes.Domain.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Clientes.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioIdempotencia : IRepositorioIdempotencia
{
    private readonly ClientesDbContext _contexto;

    public RepositorioIdempotencia(ClientesDbContext contexto)
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
