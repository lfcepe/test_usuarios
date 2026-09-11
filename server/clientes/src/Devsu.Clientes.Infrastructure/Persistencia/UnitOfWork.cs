using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Domain.Excepciones;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Clientes.Infrastructure.Persistencia;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ClientesDbContext _contexto;

    public UnitOfWork(ClientesDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<int> GuardarCambiosAsync(CancellationToken cancelacion)
    {
        try
        {
            return await _contexto.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateConcurrencyException excepcion)
        {
            throw new ConflictoConcurrenciaException(
                "El registro fue modificado por otra operacion. Vuelva a cargarlo e intente de nuevo.")
            {
                Source = excepcion.Source,
            };
        }
    }

    public async Task<T> EjecutarEnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancelacion)
    {
        // Si ya hay una transaccion abierta (por ejemplo, un consumidor que agrupa
        // varios comandos) se reutiliza en lugar de anidar otra.
        if (_contexto.Database.CurrentTransaction is not null)
        {
            return await operacion(cancelacion);
        }

        // La estrategia de ejecucion es la que sabe reintentar ante errores
        // transitorios de red. Abrir la transaccion por fuera de ella provocaria
        // el error "the configured execution strategy does not support user
        // initiated transactions".
        var estrategia = _contexto.Database.CreateExecutionStrategy();

        return await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancelacion);

            var resultado = await operacion(cancelacion);

            await transaccion.CommitAsync(cancelacion);
            return resultado;
        });
    }
}
