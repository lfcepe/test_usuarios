using Devsu.Cuentas.Domain.Comun;

namespace Devsu.Cuentas.Domain.Entidades;

/// <summary>Item de catalogo. La tabla es auto referenciada.</summary>
public class Catalogo : EntidadBase
{
    private Catalogo()
    {
    }

    public string? DetalleCatalogo { get; private set; }

    public string? Item { get; private set; }

    public int? IdRaiz { get; private set; }

    public Catalogo? Raiz { get; private set; }
}
