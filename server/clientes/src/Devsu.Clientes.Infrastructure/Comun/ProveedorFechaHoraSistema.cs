using Devsu.Clientes.Application.Comun;

namespace Devsu.Clientes.Infrastructure.Comun;

/// <summary>Reloj real del sistema, siempre en UTC.</summary>
public sealed class ProveedorFechaHoraSistema : IProveedorFechaHora
{
    public DateTime AhoraUtc => DateTime.UtcNow;

    public DateOnly HoyUtc => DateOnly.FromDateTime(DateTime.UtcNow);
}
