using System.Globalization;

namespace Devsu.Cuentas.Domain.Excepciones;

/// <summary>
/// Base de las excepciones previstas del dominio.
/// </summary>
/// <remarks>
/// Cada excepcion aporta un <see cref="Codigo"/> estable para que el consumidor
/// del API reaccione al codigo y no al texto, y un <see cref="Titulo"/> corto que
/// el middleware coloca en el campo title del ProblemDetails.
/// </remarks>
public abstract class ExcepcionDominio : Exception
{
    protected ExcepcionDominio(string mensaje, string codigo, string titulo) : base(mensaje)
    {
        Codigo = codigo;
        Titulo = titulo;
    }

    public string Codigo { get; }

    public string Titulo { get; }
}

/// <summary>El recurso solicitado no existe. Se traduce a 404.</summary>
public sealed class RecursoNoEncontradoException : ExcepcionDominio
{
    public RecursoNoEncontradoException(string mensaje)
        : base(mensaje, "RECURSO_NO_ENCONTRADO", "Recurso no encontrado")
    {
    }

    public static RecursoNoEncontradoException Cuenta(int id) =>
        new($"No existe una cuenta con el identificador {id}.");

    public static RecursoNoEncontradoException Cuenta(string numeroCuenta) =>
        new($"No existe la cuenta numero {numeroCuenta}.");

    public static RecursoNoEncontradoException Movimiento(int id) =>
        new($"No existe un movimiento con el identificador {id}.");
}

/// <summary>Se incumple una regla de negocio. Se traduce a 400.</summary>
public sealed class ReglaNegocioException : ExcepcionDominio
{
    public ReglaNegocioException(string mensaje)
        : base(mensaje, "REGLA_NEGOCIO", "No se pudo completar la operacion")
    {
    }
}

/// <summary>Ya existe un recurso con la misma clave natural. Se traduce a 409.</summary>
public sealed class RecursoDuplicadoException : ExcepcionDominio
{
    public RecursoDuplicadoException(string mensaje)
        : base(mensaje, "RECURSO_DUPLICADO", "El recurso ya existe")
    {
    }
}

/// <summary>
/// El movimiento dejaria la cuenta en descubierto. Es la funcionalidad F3 del
/// enunciado y por eso el titulo es exactamente el texto que se pide mostrar.
/// </summary>
public sealed class SaldoNoDisponibleException : ExcepcionDominio
{
    /// <summary>Texto exigido por el enunciado.</summary>
    public const string MensajeEstandar = "Saldo no disponible";

    public SaldoNoDisponibleException(string numeroCuenta, decimal saldoDisponible, decimal valorSolicitado)
        : base(
            string.Format(
                CultureInfo.InvariantCulture,
                "La cuenta {0} no cuenta con saldo suficiente. Saldo disponible: {1:0.00}, "
                + "valor solicitado: {2:0.00}.",
                numeroCuenta,
                saldoDisponible,
                valorSolicitado),
            "SALDO_NO_DISPONIBLE",
            MensajeEstandar)
    {
        NumeroCuenta = numeroCuenta;
        SaldoDisponible = saldoDisponible;
        ValorSolicitado = valorSolicitado;
    }

    public string NumeroCuenta { get; }

    public decimal SaldoDisponible { get; }

    public decimal ValorSolicitado { get; }
}

/// <summary>No se puede operar sobre una cuenta inactiva. Se traduce a 409.</summary>
public sealed class CuentaInactivaException : ExcepcionDominio
{
    public CuentaInactivaException(string numeroCuenta)
        : base(
            $"La cuenta {numeroCuenta} esta inactiva y no admite movimientos.",
            "CUENTA_INACTIVA",
            "Cuenta inactiva")
    {
    }
}

/// <summary>
/// El cliente todavia no llego a este microservicio por el canal de eventos.
/// Se traduce a 409 porque es una condicion transitoria: reintentar suele resolverla.
/// </summary>
public sealed class ClienteNoSincronizadoException : ExcepcionDominio
{
    public ClienteNoSincronizadoException(int idCliente)
        : base(
            $"El cliente {idCliente} aun no esta sincronizado en el microservicio de Cuentas "
            + "y tampoco se pudo consultar al microservicio de Clientes. Reintente en unos segundos.",
            "CLIENTE_NO_SINCRONIZADO",
            "Cliente no sincronizado")
    {
    }
}

/// <summary>El cliente existe pero esta dado de baja. Se traduce a 400.</summary>
public sealed class ClienteInactivoException : ExcepcionDominio
{
    public ClienteInactivoException(int idCliente)
        : base(
            $"El cliente {idCliente} esta inactivo y no puede operar cuentas.",
            "CLIENTE_INACTIVO",
            "Cliente inactivo")
    {
    }
}

/// <summary>Otra transaccion modifico la fila antes. Se traduce a 409.</summary>
public sealed class ConflictoConcurrenciaException : ExcepcionDominio
{
    public ConflictoConcurrenciaException(string mensaje)
        : base(mensaje, "CONFLICTO_CONCURRENCIA", "Conflicto de concurrencia")
    {
    }
}
