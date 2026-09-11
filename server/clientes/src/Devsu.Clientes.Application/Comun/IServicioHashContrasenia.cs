namespace Devsu.Clientes.Application.Comun;

/// <summary>
/// Cifra y verifica contrasenias. El algoritmo concreto es un detalle de
/// infraestructura y por eso el dominio solo maneja el hash resultante.
/// </summary>
public interface IServicioHashContrasenia
{
    string Cifrar(string contraseniaEnClaro);

    bool Verificar(string contraseniaEnClaro, string hashAlmacenado);
}
