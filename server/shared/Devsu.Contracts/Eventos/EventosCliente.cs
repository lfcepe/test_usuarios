namespace Devsu.Contracts.Eventos;

/// <summary>
/// Publicado por el microservicio de Clientes al dar de alta un cliente.
/// Lo consume Cuentas para crear la fila espejo en "ClientesRef".
/// </summary>
public sealed record ClienteCreado(
    Guid IdMensaje,
    DateTime OcurridoEn,
    int IdCliente,
    string ClienteId,
    string NombreCompleto,
    string NumeroDocumento,
    int IdEstadoCliente,
    bool Activo) : IEventoIntegracion;

/// <summary>
/// Publicado cuando cambian los datos personales o de negocio del cliente.
/// Cuentas actualiza el nombre que muestra en el reporte de estado de cuenta.
/// </summary>
public sealed record ClienteActualizado(
    Guid IdMensaje,
    DateTime OcurridoEn,
    int IdCliente,
    string ClienteId,
    string NombreCompleto,
    string NumeroDocumento,
    int IdEstadoCliente,
    bool Activo) : IEventoIntegracion;

/// <summary>
/// Publicado en la baja logica o reactivacion de un cliente.
/// Cuentas lo usa para impedir movimientos sobre cuentas de clientes inactivos.
/// </summary>
public sealed record ClienteEstadoCambiado(
    Guid IdMensaje,
    DateTime OcurridoEn,
    int IdCliente,
    int IdEstadoCliente,
    bool Activo) : IEventoIntegracion;

/// <summary>
/// Publicado en la baja definitiva de un cliente.
/// </summary>
/// <remarks>
/// Cuentas no borra el historico: marca la referencia como inactiva. Los movimientos son
/// informacion contable y deben sobrevivir a la baja del cliente.
/// </remarks>
public sealed record ClienteEliminado(
    Guid IdMensaje,
    DateTime OcurridoEn,
    int IdCliente) : IEventoIntegracion;
