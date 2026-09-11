using Devsu.Clientes.Domain.Comun;

namespace Devsu.Clientes.Domain.Entidades;

/// <summary>
/// Item de catalogo. La tabla es auto referenciada: las filas con
/// <see cref="IdRaiz"/> nulo son las cabeceras y el resto son sus items.
/// </summary>
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
