using System.Security.Cryptography;
using Devsu.Clientes.Application.Comun;

namespace Devsu.Clientes.Infrastructure.Seguridad;

/// <summary>
/// Cifrado de contrasenias con PBKDF2 y SHA-256.
/// </summary>
/// <remarks>
/// Formato almacenado: iteraciones.saltBase64.hashBase64. Guardar el numero de
/// iteraciones dentro del propio hash permite subirlo en el futuro sin invalidar
/// las contrasenias ya existentes: cada una se verifica con las suyas.
/// </remarks>
public sealed class ServicioHashContraseniaPbkdf2 : IServicioHashContrasenia
{
    private const int IteracionesActuales = 100_000;
    private const int TamanioSalt = 16;
    private const int TamanioHash = 32;
    private const char Separador = '.';

    private static readonly HashAlgorithmName Algoritmo = HashAlgorithmName.SHA256;

    public string Cifrar(string contraseniaEnClaro)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contraseniaEnClaro);

        var salt = RandomNumberGenerator.GetBytes(TamanioSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            contraseniaEnClaro,
            salt,
            IteracionesActuales,
            Algoritmo,
            TamanioHash);

        return string.Join(
            Separador,
            IteracionesActuales,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verificar(string contraseniaEnClaro, string hashAlmacenado)
    {
        if (string.IsNullOrWhiteSpace(contraseniaEnClaro) || string.IsNullOrWhiteSpace(hashAlmacenado))
        {
            return false;
        }

        var partes = hashAlmacenado.Split(Separador);
        if (partes.Length != 3 || !int.TryParse(partes[0], out var iteraciones))
        {
            return false;
        }

        byte[] salt;
        byte[] esperado;

        try
        {
            salt = Convert.FromBase64String(partes[1]);
            esperado = Convert.FromBase64String(partes[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var calculado = Rfc2898DeriveBytes.Pbkdf2(
            contraseniaEnClaro,
            salt,
            iteraciones,
            Algoritmo,
            esperado.Length);

        // Comparacion en tiempo constante: una comparacion normal tarda mas cuanto
        // mas coincidan los primeros bytes y eso es un canal lateral explotable.
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}
