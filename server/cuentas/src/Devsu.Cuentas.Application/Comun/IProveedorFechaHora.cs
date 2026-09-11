namespace Devsu.Cuentas.Application.Comun;

/// <summary>Abstrae el reloj del sistema para poder fijarlo en las pruebas.</summary>
public interface IProveedorFechaHora
{
    DateTime AhoraUtc { get; }

    DateOnly HoyUtc { get; }
}
