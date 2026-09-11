namespace Devsu.Contracts.Eventos;

/// <summary>
/// Contrato comun de los eventos que viajan entre microservicios.
/// </summary>
/// <remarks>
/// IdMensaje es la clave de idempotencia: el consumidor la registra en "MensajesProcesados"
/// y descarta cualquier reentrega. RabbitMQ garantiza entrega "al menos una vez", no "exactamente
/// una vez", asi que sin esta clave un reintento duplicaria el efecto del evento.
/// </remarks>
public interface IEventoIntegracion
{
    Guid IdMensaje { get; }

    DateTime OcurridoEn { get; }
}
