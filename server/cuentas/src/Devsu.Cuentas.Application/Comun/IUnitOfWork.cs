namespace Devsu.Cuentas.Application.Comun;

/// <summary>
/// Confirma en un unico commit todos los cambios acumulados por los repositorios.
/// </summary>
/// <remarks>
/// Los manejadores no llaman a SaveChanges del DbContext. Al pasar por aqui, el
/// cambio de negocio y el evento encolado en el outbox entran en la misma
/// transaccion, que es lo que hace fiable la publicacion de eventos.
/// </remarks>
public interface IUnitOfWork
{
    Task<int> GuardarCambiosAsync(CancellationToken cancelacion);

    /// <summary>
    /// Ejecuta la operacion dentro de una transaccion explicita.
    /// </summary>
    /// <remarks>
    /// Hace falta cuando el caso de uso necesita guardar dos veces: una para que
    /// la base genere el identificador y otra para encolar el evento que lo lleva
    /// dentro. Sin la transaccion, un fallo entre ambas dejaria un cliente creado
    /// sin evento publicado y las dos bases desincronizadas.
    /// </remarks>
    Task<T> EjecutarEnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancelacion);
}
