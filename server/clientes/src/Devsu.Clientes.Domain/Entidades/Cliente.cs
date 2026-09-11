using Devsu.Clientes.Domain.Catalogos;
using Devsu.Clientes.Domain.Comun;
using Devsu.Clientes.Domain.Excepciones;

namespace Devsu.Clientes.Domain.Entidades;

/// <summary>
/// Cliente del banco. Hereda de <see cref="Persona"/> tal como exige el enunciado
/// y se mapea a la tabla "Cliente" con estrategia Table-Per-Type.
/// </summary>
public sealed class Cliente : Persona
{
    /// <summary>Literal que deja datos-prueba.sql donde no puede calcular el hash.</summary>
    public const string ContraseniaPendiente = "__PENDIENTE_HASH__";

    private const int LongitudMinimaContrasenia = 4;

    private Cliente()
    {
    }

    private Cliente(DatosPersonales datos, string clienteId, string contraseniaHash, bool activo)
        : base(datos, CatalogoIds.EstadoPersona.Desde(activo))
    {
        ClienteId = ValidarClienteId(clienteId);
        Contrasenia = ValidarHash(contraseniaHash);
        IdEstadoCliente = CatalogoIds.EstadoCliente.Desde(activo);
    }

    /// <summary>Clave unica de negocio con formato CLI-000001.</summary>
    public string ClienteId { get; private set; } = null!;

    /// <summary>Hash PBKDF2 de la contrasenia. Nunca contiene la clave en claro.</summary>
    public string Contrasenia { get; private set; } = null!;

    public int IdEstadoCliente { get; private set; }

    public Catalogo? EstadoCliente { get; private set; }

    public bool Activo => IdEstadoCliente == CatalogoIds.EstadoCliente.Activo;

    /// <summary>
    /// Da de alta un cliente. La contrasenia debe llegar ya cifrada: el dominio no
    /// conoce el algoritmo de hash, eso es responsabilidad de la infraestructura.
    /// </summary>
    public static Cliente Crear(DatosPersonales datos, string clienteId, string contraseniaHash, bool activo = true) =>
        new(datos, clienteId, contraseniaHash, activo);

    /// <summary>Da formato a la clave de negocio a partir de un consecutivo.</summary>
    public static string FormatearClienteId(int consecutivo)
    {
        if (consecutivo <= 0)
        {
            throw new ReglaNegocioException("El consecutivo del cliente debe ser mayor que cero.");
        }

        return $"CLI-{consecutivo:D6}";
    }

    /// <summary>Valida la longitud minima de la clave antes de cifrarla.</summary>
    /// <remarks>
    /// Vive en el dominio para que la regla sea la misma venga de donde venga la
    /// peticion. El minimo es de cuatro caracteres porque los datos del enunciado
    /// usan claves como "1234"; en un sistema real seria mucho mas exigente.
    /// </remarks>
    public static void ValidarContraseniaEnClaro(string? contrasenia)
    {
        if (string.IsNullOrWhiteSpace(contrasenia) || contrasenia.Trim().Length < LongitudMinimaContrasenia)
        {
            throw new ReglaNegocioException(
                $"La contrasenia debe tener al menos {LongitudMinimaContrasenia} caracteres.");
        }
    }

    /// <summary>Activa o desactiva el cliente y su persona asociada.</summary>
    public void CambiarEstado(bool activo)
    {
        IdEstadoCliente = CatalogoIds.EstadoCliente.Desde(activo);
        CambiarEstadoPersona(activo);
    }

    public void CambiarContrasenia(string nuevoHash) => Contrasenia = ValidarHash(nuevoHash);

    /// <summary>Reemplaza los datos personales conservando la identidad del cliente.</summary>
    public void Actualizar(DatosPersonales datos, bool activo)
    {
        AplicarDatosPersonales(datos);
        CambiarEstado(activo);
    }

    /// <summary>Indica si la fila todavia arrastra el marcador de datos-prueba.sql.</summary>
    public bool RequiereHashInicial() =>
        string.Equals(Contrasenia, ContraseniaPendiente, StringComparison.Ordinal);

    private static string ValidarClienteId(string clienteId) =>
        Guardas.TextoRequerido(clienteId, nameof(ClienteId), 20);

    // El hash va en Base64 y distingue mayusculas de minusculas: normalizarlo como
    // el resto del texto lo dejaria inservible.
    private static string ValidarHash(string contraseniaHash) =>
        Guardas.TextoSensible(contraseniaHash, nameof(Contrasenia), 256);
}
