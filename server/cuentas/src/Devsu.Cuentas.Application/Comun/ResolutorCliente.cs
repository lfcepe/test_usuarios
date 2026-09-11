using Devsu.Cuentas.Domain.Entidades;
using Devsu.Cuentas.Domain.Excepciones;
using Devsu.Cuentas.Domain.Repositorios;
using Microsoft.Extensions.Logging;

namespace Devsu.Cuentas.Application.Comun;

/// <summary>
/// Obtiene la referencia local del cliente y, si todavia no llego por eventos, la
/// recupera del microservicio de Clientes.
/// </summary>
/// <remarks>
/// Este es el punto donde se materializa la consistencia eventual del sistema. El
/// orden importa:
///   1. se busca en "ClientesRef", que es lo normal y no cuesta nada;
///   2. si no esta, se consulta la API de Clientes, que puede fallar;
///   3. si la consulta funciona, se guarda la referencia para no repetirla;
///   4. si tampoco responde, se rechaza la operacion con un 409, porque abrir una
///      cuenta a un cliente que no se ha podido verificar seria peor que fallar.
/// </remarks>
public sealed class ResolutorCliente
{
    private readonly IRepositorioClienteRef _clientes;
    private readonly IClientesApiClient _clientesApi;
    private readonly IProveedorFechaHora _reloj;
    private readonly ILogger<ResolutorCliente> _log;

    public ResolutorCliente(
        IRepositorioClienteRef clientes,
        IClientesApiClient clientesApi,
        IProveedorFechaHora reloj,
        ILogger<ResolutorCliente> log)
    {
        _clientes = clientes;
        _clientesApi = clientesApi;
        _reloj = reloj;
        _log = log;
    }

    /// <summary>Devuelve el cliente y exige que este activo.</summary>
    public async Task<ClienteRef> ObtenerActivoAsync(int idCliente, CancellationToken cancelacion)
    {
        var cliente = await ObtenerAsync(idCliente, cancelacion);

        if (!cliente.Activo)
        {
            throw new ClienteInactivoException(idCliente);
        }

        return cliente;
    }

    public async Task<ClienteRef> ObtenerAsync(int idCliente, CancellationToken cancelacion)
    {
        var local = await _clientes.ObtenerPorIdAsync(idCliente, cancelacion);

        if (local is not null)
        {
            return local;
        }

        _log.LogWarning(
            "El cliente {IdCliente} no esta en ClientesRef. Se consulta el microservicio de Clientes",
            idCliente);

        var remoto = await ConsultarRemotoAsync(idCliente, cancelacion)
            ?? throw new ClienteNoSincronizadoException(idCliente);

        var referencia = ClienteRef.Crear(
            remoto.Id,
            remoto.ClienteId,
            remoto.NombreCompleto,
            remoto.NumeroDocumento,
            remoto.IdEstadoCliente,
            remoto.Estado,
            _reloj.AhoraUtc);

        await _clientes.AgregarAsync(referencia, cancelacion);

        return referencia;
    }

    private async Task<ClienteRemoto?> ConsultarRemotoAsync(int idCliente, CancellationToken cancelacion)
    {
        try
        {
            return await _clientesApi.ObtenerClienteAsync(idCliente, cancelacion);
        }
        catch (Exception excepcion)
        {
            // El cortacircuitos abierto tambien llega hasta aqui. Se registra y se
            // deja que el llamador convierta la ausencia de dato en un 409.
            _log.LogError(
                excepcion,
                "Fallo la consulta de respaldo del cliente {IdCliente} contra el microservicio de Clientes",
                idCliente);

            return null;
        }
    }
}
