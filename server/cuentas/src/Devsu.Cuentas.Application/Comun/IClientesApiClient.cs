namespace Devsu.Cuentas.Application.Comun;

/// <summary>Datos del cliente tal como los devuelve el microservicio de Clientes.</summary>
public sealed record ClienteRemoto(
    int Id,
    string ClienteId,
    string NombreCompleto,
    string NumeroDocumento,
    int IdEstadoCliente,
    bool Estado);

/// <summary>
/// Consulta sincrona de respaldo contra el microservicio de Clientes.
/// </summary>
/// <remarks>
/// El camino normal es asincrono: los eventos mantienen "ClientesRef" al dia. Este
/// cliente HTTP solo se usa cuando la referencia todavia no llego (arranque en
/// frio, evento en vuelo) y hay que decidir si una cuenta se puede abrir. Va
/// protegido con reintentos y cortacircuitos; si tampoco responde, el caso de uso
/// falla de forma explicita en lugar de inventarse los datos del titular.
/// </remarks>
public interface IClientesApiClient
{
    Task<ClienteRemoto?> ObtenerClienteAsync(int idCliente, CancellationToken cancelacion);
}
