namespace Devsu.Clientes.Application.Clientes.Contratos;

/// <summary>Cuerpo esperado por POST /api/clientes.</summary>
public sealed record CrearClienteRequest(
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
    DateOnly FechaNacimiento,
    string Contrasenia,
    bool Estado = true);
