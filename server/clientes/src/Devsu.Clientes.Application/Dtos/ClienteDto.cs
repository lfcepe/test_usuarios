namespace Devsu.Clientes.Application.Dtos;

/// <summary>Representacion de un cliente para el exterior del microservicio.</summary>
public sealed record ClienteDto(
    int Id,
    string ClienteId,
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
    int IdEstadoCliente,
    string? EstadoDescripcion,
    int TotalCuentas,
    DateTime FechaCreacion,
    DateTime? FechaModificacion);
