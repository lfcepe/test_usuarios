namespace Devsu.Clientes.Domain.Excepciones;

/// <summary>
/// Base de las excepciones previstas del dominio. El middleware de la API las
/// traduce a ProblemDetails usando <see cref="Codigo"/>, de modo que el cliente
/// HTTP puede reaccionar al codigo en lugar de parsear el mensaje.
/// </summary>
public abstract class ExcepcionDominio : Exception
{
    protected ExcepcionDominio(string mensaje, string codigo) : base(mensaje)
    {
        Codigo = codigo;
    }

    public string Codigo { get; }
}

/// <summary>El recurso solicitado no existe. Se traduce a 404.</summary>
public sealed class RecursoNoEncontradoException : ExcepcionDominio
{
    public RecursoNoEncontradoException(string mensaje)
        : base(mensaje, "RECURSO_NO_ENCONTRADO")
    {
    }

    public static RecursoNoEncontradoException Cliente(int id) =>
        new($"No existe un cliente con el identificador {id}.");

    public static RecursoNoEncontradoException Persona(int id) =>
        new($"No existe una persona con el identificador {id}.");
}

/// <summary>Se incumple una regla de negocio. Se traduce a 400.</summary>
public sealed class ReglaNegocioException : ExcepcionDominio
{
    public ReglaNegocioException(string mensaje)
        : base(mensaje, "REGLA_NEGOCIO")
    {
    }
}

/// <summary>Ya existe un recurso con la misma clave natural. Se traduce a 409.</summary>
public sealed class RecursoDuplicadoException : ExcepcionDominio
{
    public RecursoDuplicadoException(string mensaje)
        : base(mensaje, "RECURSO_DUPLICADO")
    {
    }
}

/// <summary>Otra transaccion modifico la fila antes. Se traduce a 409.</summary>
public sealed class ConflictoConcurrenciaException : ExcepcionDominio
{
    public ConflictoConcurrenciaException(string mensaje)
        : base(mensaje, "CONFLICTO_CONCURRENCIA")
    {
    }
}
