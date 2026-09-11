namespace Devsu.Clientes.Domain.Comun;

/// <summary>Raiz de todas las entidades persistidas, identificadas por un entero.</summary>
public abstract class EntidadBase
{
    public int Id { get; protected set; }
}

/// <summary>Entidad que ademas lleva rastro de cuando se creo y se modifico.</summary>
public abstract class EntidadAuditable : EntidadBase
{
    public DateTime FechaCreacion { get; protected set; }

    public DateTime? FechaModificacion { get; protected set; }

    public void RegistrarCreacion(DateTime momento) => FechaCreacion = momento;

    public void RegistrarModificacion(DateTime momento) => FechaModificacion = momento;
}
