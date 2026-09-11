namespace Devsu.Clientes.Application.Clientes.Contratos;

/// <summary>
/// Cuerpo esperado por PUT /api/clientes/{id}.
/// </summary>
/// <remarks>
/// La contrasenia es opcional: si llega vacia o nula se conserva la actual. Es la
/// alternativa a obligar al cliente a reenviar el hash, que no conoce.
/// </remarks>
public sealed record ActualizarClienteRequest(
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
    string? Contrasenia,
    bool Estado);
