namespace Devsu.Clientes.Domain.Entidades;

/// <summary>
/// Agrupa los datos personales que comparten Persona y Cliente.
/// </summary>
/// <remarks>
/// Existe para no arrastrar un constructor de once parametros posicionales, donde
/// invertir dos strings del mismo tipo compila igual y falla en produccion.
/// </remarks>
public sealed record DatosPersonales(
    string PrimerNombre,
    string? SegundoNombre,
    string PrimerApellido,
    string? SegundoApellido,
    int IdTipoDocumento,
    string NumeroDocumento,
    int IdGenero,
    string DireccionDomicilio,
    string NumeroCelular,
    string Email,
    DateOnly FechaNacimiento);
