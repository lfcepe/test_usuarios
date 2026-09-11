namespace Devsu.Clientes.Application.Clientes.Contratos;

/// <summary>Cuerpo esperado por PATCH /api/clientes/{id}/estado.</summary>
public sealed record CambiarEstadoRequest(bool Estado);
