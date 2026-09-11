namespace Devsu.Clientes.Domain.Entidades;

/// <summary>
/// Numero de cuentas ACTIVAS que tiene un cliente. Es un read model: lo mantiene el
/// consumidor de los eventos que publica el microservicio de Cuentas.
/// </summary>
/// <remarks>
/// Evita una llamada sincrona al otro microservicio cada vez que se lista un
/// cliente. El precio es consistencia eventual: entre la apertura de la cuenta y
/// la llegada del evento el contador puede ir un instante por detras.
/// </remarks>
public class ResumenCuentasCliente
{
    private ResumenCuentasCliente()
    {
    }

    public int IdCliente { get; private set; }

    public int TotalCuentas { get; private set; }

    public DateTime FechaActualizacion { get; private set; }

    public static ResumenCuentasCliente Crear(int idCliente, DateTime momento) => new()
    {
        IdCliente = idCliente,
        TotalCuentas = 0,
        FechaActualizacion = momento,
    };

    public void Incrementar(DateTime momento)
    {
        TotalCuentas++;
        FechaActualizacion = momento;
    }

    public void Decrementar(DateTime momento)
    {
        // Nunca baja de cero: si llegara un evento de baja sin su alta previa,
        // preferimos un contador conservador antes que un numero negativo en pantalla.
        TotalCuentas = TotalCuentas > 0 ? TotalCuentas - 1 : 0;
        FechaActualizacion = momento;
    }

    public void Establecer(int total, DateTime momento)
    {
        TotalCuentas = total < 0 ? 0 : total;
        FechaActualizacion = momento;
    }
}
