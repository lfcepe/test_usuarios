namespace Devsu.Clientes.Domain.Entidades;

/// <summary>
/// Evento de integracion pendiente de publicar, guardado en la misma transaccion
/// que el cambio de negocio que lo origino (patron Transactional Outbox).
/// </summary>
public class OutboxMensaje
{
    private OutboxMensaje()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Nombre completo del tipo, usado para deserializar al publicar.</summary>
    public string TipoMensaje { get; private set; } = null!;

    public string Contenido { get; private set; } = null!;

    public DateTime FechaCreacion { get; private set; }

    public DateTime? FechaProcesado { get; private set; }

    public int Intentos { get; private set; }

    public string? Error { get; private set; }

    public static OutboxMensaje Crear(Guid id, string tipoMensaje, string contenido, DateTime momento) => new()
    {
        Id = id,
        TipoMensaje = tipoMensaje,
        Contenido = contenido,
        FechaCreacion = momento,
    };

    public void MarcarProcesado(DateTime momento)
    {
        FechaProcesado = momento;
        Error = null;
    }

    public void RegistrarFallo(string error)
    {
        Intentos++;
        // Se recorta porque algunos errores de transporte traen la traza completa
        // y no aporta nada guardar mas de esto en cada reintento.
        Error = error.Length > 2000 ? error[..2000] : error;
    }
}
