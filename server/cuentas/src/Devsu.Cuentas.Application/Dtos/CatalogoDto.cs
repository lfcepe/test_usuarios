namespace Devsu.Cuentas.Application.Dtos;

/// <summary>Item de catalogo tal como lo consume el frontend.</summary>
public sealed record CatalogoDto(
    int Id,
    string? DetalleCatalogo,
    string? Item,
    int? IdRaiz);
