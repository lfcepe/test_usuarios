using Devsu.Clientes.Domain.Catalogos;
using Devsu.Clientes.Domain.Comun;
using Devsu.Clientes.Domain.Excepciones;

namespace Devsu.Clientes.Domain.Entidades;

/// <summary>
/// Datos personales de un individuo. Es la clase base de <see cref="Cliente"/>
/// y se mapea a la tabla "Personas".
/// </summary>
public class Persona : EntidadAuditable
{
    /// <summary>Constructor sin parametros exigido por Entity Framework Core.</summary>
    protected Persona()
    {
    }

    protected Persona(DatosPersonales datos, int idEstadoPersona)
    {
        AplicarDatosPersonales(datos);
        IdEstadoPersona = Guardas.IdCatalogoValido(idEstadoPersona, nameof(IdEstadoPersona));
    }

    public string PrimerNombre { get; private set; } = null!;

    public string? SegundoNombre { get; private set; }

    public string PrimerApellido { get; private set; } = null!;

    public string? SegundoApellido { get; private set; }

    public int IdTipoDocumento { get; private set; }

    public string NumeroDocumento { get; private set; } = null!;

    public int IdGenero { get; private set; }

    public string DireccionDomicilio { get; private set; } = null!;

    public string NumeroCelular { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public DateOnly FechaNacimiento { get; private set; }

    /// <summary>
    /// Edad en anios cumplidos. La mantiene el trigger tr_personas_edad de PostgreSQL.
    /// </summary>
    /// <remarks>
    /// La aplicacion nunca escribe esta columna: EF la declara como generada por el
    /// almacen y la recupera con RETURNING. El motivo de dejarla en la base es que la
    /// edad cambia sola con el paso del tiempo, y un valor calculado en C# en el
    /// momento del alta quedaria obsoleto al dia siguiente del cumpleanios.
    /// </remarks>
    public int? Edad { get; private set; }

    public int IdEstadoPersona { get; protected set; }

    public Catalogo? TipoDocumento { get; private set; }

    public Catalogo? Genero { get; private set; }

    public Catalogo? EstadoPersona { get; private set; }

    /// <summary>Nombre y apellidos concatenados, omitiendo los que no existan.</summary>
    public string NombreCompleto =>
        string.Join(' ', new[] { PrimerNombre, SegundoNombre, PrimerApellido, SegundoApellido }
            .Where(parte => !string.IsNullOrWhiteSpace(parte)));

    public bool Activa => IdEstadoPersona == CatalogoIds.EstadoPersona.Activo;

    public static Persona Crear(DatosPersonales datos, bool activa = true) =>
        new(datos, CatalogoIds.EstadoPersona.Desde(activa));

    /// <summary>Edad en anios cumplidos a una fecha dada.</summary>
    /// <remarks>
    /// Replica exactamente lo que hace fn_calcular_edad en PostgreSQL. Se usa en las
    /// pruebas y como respaldo cuando el proveedor de datos no es PostgreSQL y por
    /// tanto no hay trigger (por ejemplo, SQLite en las pruebas de integracion).
    /// </remarks>
    public static int CalcularEdad(DateOnly fechaNacimiento, DateOnly hoy)
    {
        if (fechaNacimiento > hoy)
        {
            throw new ReglaNegocioException("La fecha de nacimiento no puede ser posterior a la fecha actual.");
        }

        var edad = hoy.Year - fechaNacimiento.Year;
        if (fechaNacimiento.AddYears(edad) > hoy)
        {
            edad--;
        }

        return edad;
    }

    public void AplicarDatosPersonales(DatosPersonales datos)
    {
        ArgumentNullException.ThrowIfNull(datos);

        PrimerNombre = Guardas.TextoRequerido(datos.PrimerNombre, nameof(PrimerNombre), 64);
        SegundoNombre = Guardas.TextoOpcional(datos.SegundoNombre, nameof(SegundoNombre), 64);
        PrimerApellido = Guardas.TextoRequerido(datos.PrimerApellido, nameof(PrimerApellido), 64);
        SegundoApellido = Guardas.TextoOpcional(datos.SegundoApellido, nameof(SegundoApellido), 64);
        IdTipoDocumento = Guardas.IdCatalogoValido(datos.IdTipoDocumento, nameof(IdTipoDocumento));
        NumeroDocumento = Guardas.Documento(datos.NumeroDocumento);
        IdGenero = Guardas.IdCatalogoValido(datos.IdGenero, nameof(IdGenero));
        DireccionDomicilio = Guardas.TextoRequerido(datos.DireccionDomicilio, nameof(DireccionDomicilio), 1000);
        NumeroCelular = Guardas.SoloDigitos(datos.NumeroCelular, nameof(NumeroCelular), 10);
        Email = Guardas.Email(datos.Email);
        FechaNacimiento = ValidarFechaNacimiento(datos.FechaNacimiento);
    }

    public void CambiarEstadoPersona(bool activa) =>
        IdEstadoPersona = CatalogoIds.EstadoPersona.Desde(activa);

    /// <summary>
    /// Fija la edad cuando el almacen no puede calcularla por si mismo.
    /// </summary>
    /// <remarks>
    /// Solo la invoca el DbContext para proveedores sin el trigger. En PostgreSQL
    /// el valor lo escribe la base y este metodo no llega a llamarse.
    /// </remarks>
    public void EstablecerEdad(int edad) => Edad = edad;

    private static DateOnly ValidarFechaNacimiento(DateOnly fechaNacimiento)
    {
        if (fechaNacimiento == default)
        {
            throw new ReglaNegocioException("La fecha de nacimiento es obligatoria.");
        }

        // Se compara contra la fecha del sistema y no contra un IProveedorFechaHora
        // inyectado porque una fecha de nacimiento futura es un error de captura,
        // no una regla de negocio que dependa de la zona horaria del servidor.
        if (fechaNacimiento > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ReglaNegocioException("La fecha de nacimiento no puede ser posterior a la fecha actual.");
        }

        if (fechaNacimiento.Year < 1900)
        {
            throw new ReglaNegocioException("La fecha de nacimiento no es valida.");
        }

        return fechaNacimiento;
    }
}
