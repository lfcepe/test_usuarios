namespace Devsu.Clientes.Infrastructure.Mensajeria;

/// <summary>Configuracion del broker, seccion "Mensajeria" de appsettings.</summary>
public sealed class OpcionesMensajeria
{
    public const string Seccion = "Mensajeria";

    /// <summary>
    /// Permite apagar por completo la mensajeria.
    /// </summary>
    /// <remarks>
    /// Las pruebas de integracion lo ponen en false para no necesitar un RabbitMQ
    /// levantado. El resto de la aplicacion funciona igual: los eventos se siguen
    /// escribiendo en el outbox, simplemente nadie los saca de ahi.
    /// </remarks>
    public bool Habilitado { get; set; } = true;

    public string Host { get; set; } = "localhost";

    public ushort Puerto { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string Usuario { get; set; } = "guest";

    public string Contrasenia { get; set; } = "guest";

    /// <summary>Nombre de la cola de este servicio. Debe ser estable entre despliegues.</summary>
    public string Cola { get; set; } = "devsu-clientes";

    /// <summary>Cada cuantos segundos el publicador vacia el outbox.</summary>
    public int IntervaloOutboxSegundos { get; set; } = 5;

    public int LoteOutbox { get; set; } = 50;
}
