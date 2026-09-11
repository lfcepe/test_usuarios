using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Domain.Entidades;
using Devsu.Clientes.Domain.Repositorios;
using Microsoft.Extensions.Logging;

namespace Devsu.Clientes.Infrastructure.Persistencia;

/// <summary>
/// Sustituye el marcador que deja datos-prueba.sql por el hash real de la clave.
/// </summary>
/// <remarks>
/// El script de datos de prueba no puede calcular PBKDF2, asi que inserta el
/// literal __PENDIENTE_HASH__ en la columna "Contrasenia". Al arrancar, este
/// componente busca esas filas y las cifra con las claves del enunciado. Solo
/// afecta a los tres clientes de ejemplo; cualquier otro cliente se crea ya con
/// su hash desde el caso de uso de alta.
/// </remarks>
public sealed class SeedContrasenias
{
    private static readonly IReadOnlyDictionary<string, string> ClavesDelEnunciado =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CLI-000001"] = "1234",
            ["CLI-000002"] = "5678",
            ["CLI-000003"] = "1245",
        };

    private readonly IRepositorioCliente _clientes;
    private readonly IServicioHashContrasenia _hash;
    private readonly IUnitOfWork _unidadTrabajo;
    private readonly ILogger<SeedContrasenias> _log;

    public SeedContrasenias(
        IRepositorioCliente clientes,
        IServicioHashContrasenia hash,
        IUnitOfWork unidadTrabajo,
        ILogger<SeedContrasenias> log)
    {
        _clientes = clientes;
        _hash = hash;
        _unidadTrabajo = unidadTrabajo;
        _log = log;
    }

    public async Task EjecutarAsync(CancellationToken cancelacion)
    {
        var pendientes = await _clientes.ObtenerConContraseniaPendienteAsync(cancelacion);

        if (pendientes.Count == 0)
        {
            return;
        }

        foreach (var cliente in pendientes)
        {
            var claveEnClaro = ClavesDelEnunciado.TryGetValue(cliente.ClienteId, out var clave)
                ? clave
                : GenerarClaveAleatoria();

            cliente.CambiarContrasenia(_hash.Cifrar(claveEnClaro));
        }

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _log.LogInformation(
            "Se cifraron {Total} contrasenias de los datos de prueba",
            pendientes.Count);
    }

    /// <summary>
    /// Clave imposible de adivinar para filas sembradas fuera del enunciado.
    /// </summary>
    /// <remarks>
    /// Es preferible dejar la cuenta inaccesible antes que asignarle una clave por
    /// defecto conocida, que es como se cuelan credenciales debiles en produccion.
    /// </remarks>
    private static string GenerarClaveAleatoria() => Guid.NewGuid().ToString("N");
}
