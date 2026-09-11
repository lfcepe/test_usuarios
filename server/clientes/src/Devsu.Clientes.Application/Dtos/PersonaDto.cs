namespace Devsu.Clientes.Application.Dtos;

/// <summary>Representacion de una persona, sea o no cliente.</summary>
public sealed record PersonaDto(
    int Id,
    string PrimerNombre,
    string? SegundoNombre,
    string PrimerApellido,
    string? SegundoApellido,
    string NombreCompleto,
    int IdTipoDocumento,
    string? TipoDocumento,
    string NumeroDocumento,
    int IdGenero,
    string? Genero,
    string DireccionDomicilio,
    string NumeroCelular,
    string Email,
    DateOnly FechaNacimiento,
    int? Edad,
    bool Estado,
    DateTime FechaCreacion,
    DateTime? FechaModificacion);
