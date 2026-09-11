namespace Devsu.Clientes.Application.Dtos;

/// <summary>Item de catalogo tal como lo consume el frontend para poblar selectores.</summary>
public sealed record CatalogoDto(
    int Id,
    string? DetalleCatalogo,
    string? Item,
    int? IdRaiz);
