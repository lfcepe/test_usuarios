using Devsu.Cuentas.Domain.Catalogos;
using Devsu.Cuentas.Domain.Comun;

namespace Devsu.Cuentas.Domain.Entidades;

/// <summary>
/// Copia local de los datos del cliente que este microservicio necesita.
/// </summary>
/// <remarks>
/// No es la fuente de verdad: la mantiene el consumidor de los eventos que publica
/// el microservicio de Clientes. Existe para que el reporte de estado de cuenta
/// pueda mostrar el nombre del titular sin una llamada sincrona y para poder
/// validar la existencia del cliente aunque el otro servicio este caido.
/// </remarks>
public class ClienteRef
{
    private ClienteRef()
    {
    }

    public int IdCliente { get; private set; }

    public string ClienteId { get; private set; } = null!;

    public string NombreCompleto { get; private set; } = null!;

    public string NumeroDocumento { get; private set; } = null!;

    public int IdEstadoCliente { get; private set; }

    public bool Activo { get; private set; }

    public DateTime FechaSincronizacion { get; private set; }

    public static ClienteRef Crear(
        int idCliente,
        string clienteId,
        string nombreCompleto,
        string numeroDocumento,
        int idEstadoCliente,
        bool activo,
        DateTime momento) => new()
        {
            IdCliente = idCliente,
            ClienteId = Normalizador.ATextoNormalizado(clienteId),
            NombreCompleto = Normalizador.ATextoNormalizado(nombreCompleto),
            NumeroDocumento = Normalizador.ATextoNormalizado(numeroDocumento),
            IdEstadoCliente = idEstadoCliente,
            Activo = activo,
            FechaSincronizacion = momento,
        };

    public void Actualizar(
        string clienteId,
        string nombreCompleto,
        string numeroDocumento,
        int idEstadoCliente,
        bool activo,
        DateTime momento)
    {
        ClienteId = Normalizador.ATextoNormalizado(clienteId);
        NombreCompleto = Normalizador.ATextoNormalizado(nombreCompleto);
        NumeroDocumento = Normalizador.ATextoNormalizado(numeroDocumento);
        IdEstadoCliente = idEstadoCliente;
        Activo = activo;
        FechaSincronizacion = momento;
    }

    public void CambiarEstado(bool activo, DateTime momento)
    {
        Activo = activo;
        IdEstadoCliente = activo ? CatalogoIds.EstadoCliente.Activo : CatalogoIds.EstadoCliente.Inactivo;
        FechaSincronizacion = momento;
    }
}
