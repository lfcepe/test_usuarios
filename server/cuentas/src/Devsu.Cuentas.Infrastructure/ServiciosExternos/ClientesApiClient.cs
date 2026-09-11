using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Devsu.Cuentas.Application.Comun;
using Microsoft.Extensions.Logging;

namespace Devsu.Cuentas.Infrastructure.ServiciosExternos;

/// <summary>
/// Cliente HTTP hacia el microservicio de Clientes.
/// </summary>
/// <remarks>
/// Es el unico punto de acoplamiento sincrono entre los dos servicios y se usa
/// solo como respaldo cuando la replica local todavia no tiene al cliente. Va
/// envuelto en la politica de resiliencia estandar (reintentos con espera
/// exponencial, cortacircuitos y tiempo limite) que se configura en el registro
/// de dependencias.
/// </remarks>
public sealed class ClientesApiClient : IClientesApiClient
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<ClientesApiClient> _log;

    public ClientesApiClient(HttpClient http, ILogger<ClientesApiClient> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<ClienteRemoto?> ObtenerClienteAsync(int idCliente, CancellationToken cancelacion)
    {
        var respuesta = await _http.GetAsync($"api/clientes/{idCliente}", cancelacion);

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            // Que no exista no es un fallo de comunicacion: es una respuesta valida
            // que significa que el cliente no esta registrado en el otro servicio.
            _log.LogInformation("El microservicio de Clientes no conoce al cliente {IdCliente}", idCliente);
            return null;
        }

        respuesta.EnsureSuccessStatusCode();

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaCliente>(OpcionesJson, cancelacion);

        if (cuerpo is null)
        {
            return null;
        }

        return new ClienteRemoto(
            cuerpo.Id,
            cuerpo.ClienteId,
            cuerpo.NombreCompleto,
            cuerpo.NumeroDocumento,
            cuerpo.IdEstadoCliente,
            cuerpo.Estado);
    }

    /// <summary>
    /// Proyeccion de la respuesta del otro microservicio.
    /// </summary>
    /// <remarks>
    /// Se declara aqui, y no se reutiliza su DTO, precisamente para no acoplar los
    /// dos servicios: este contrato recoge solo los campos que Cuentas necesita y
    /// puede sobrevivir a que el otro anada o reordene propiedades.
    /// </remarks>
    private sealed record RespuestaCliente(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("clienteId")] string ClienteId,
        [property: JsonPropertyName("nombreCompleto")] string NombreCompleto,
        [property: JsonPropertyName("numeroDocumento")] string NumeroDocumento,
        [property: JsonPropertyName("idEstadoCliente")] int IdEstadoCliente,
        [property: JsonPropertyName("estado")] bool Estado);
}
