namespace Devsu.Cuentas.Domain.Entidades;

/// <summary>
/// Huella de un evento ya consumido. Garantiza que una reentrega del broker no
/// vuelva a aplicar el mismo efecto (patron Inbox).
/// </summary>
public class MensajeProcesado
{
    private MensajeProcesado()
    {
    }

    public Guid IdMensaje { get; private set; }

    public string TipoMensaje { get; private set; } = null!;

    public DateTime FechaProcesado { get; private set; }

    public static MensajeProcesado Crear(Guid idMensaje, string tipoMensaje, DateTime momento) => new()
    {
        IdMensaje = idMensaje,
        TipoMensaje = tipoMensaje,
        FechaProcesado = momento,
    };
}
