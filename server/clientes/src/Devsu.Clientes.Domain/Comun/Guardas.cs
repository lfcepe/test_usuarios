using System.Text.RegularExpressions;
using Devsu.Clientes.Domain.Excepciones;

namespace Devsu.Clientes.Domain.Comun;

/// <summary>
/// Validaciones de invariantes de dominio. Se ejecutan dentro de las entidades,
/// no en los validadores de FluentValidation.
/// </summary>
/// <remarks>
/// La duplicidad con FluentValidation es intencional: el validador protege el
/// borde HTTP y devuelve 422 con el detalle de los errores; estas guardas
/// protegen la entidad de cualquier otra ruta de entrada (un consumidor de
/// eventos, un seed, una prueba) y garantizan que no exista un Cliente invalido
/// en memoria bajo ninguna circunstancia.
///
/// Ademas de validar, normalizan: todo texto de negocio se almacena en mayusculas
/// y sin espacios sobrantes. Ver <see cref="Normalizador"/>.
/// </remarks>
public static class Guardas
{
    private static readonly Regex FormatoEmail =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);

    public static string TextoRequerido(string? valor, string campo, int longitudMaxima)
    {
        var limpio = ValidarPresenciaYLongitud(valor, campo, longitudMaxima);

        return Normalizador.ATextoNormalizado(limpio);
    }

    public static string? TextoOpcional(string? valor, string campo, int longitudMaxima)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return TextoRequerido(valor, campo, longitudMaxima);
    }

    /// <summary>Valida sin normalizar a mayusculas, para valores donde el caso importa.</summary>
    public static string TextoSensible(string? valor, string campo, int longitudMaxima)
    {
        var limpio = ValidarPresenciaYLongitud(valor, campo, longitudMaxima);

        return Normalizador.ATextoLimpio(limpio);
    }

    public static string Email(string? valor)
    {
        var limpio = TextoRequerido(valor, "Email", 500);

        if (!FormatoEmail.IsMatch(limpio))
        {
            throw new ReglaNegocioException($"El correo electronico '{limpio}' no tiene un formato valido.");
        }

        return limpio;
    }

    public static string SoloDigitos(string? valor, string campo, int longitudExacta)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException($"El campo {campo} es obligatorio.");
        }

        // No se delega en TextoRequerido a proposito: si el valor excede la longitud,
        // el mensaje util es el de los digitos exactos y no uno generico de longitud.
        var limpio = valor.Trim();
        if (limpio.Length != longitudExacta || !limpio.All(char.IsDigit))
        {
            throw new ReglaNegocioException(
                $"El campo {campo} debe contener exactamente {longitudExacta} digitos.");
        }

        return limpio;
    }

    public static string Documento(string? valor)
    {
        var limpio = TextoRequerido(valor, "NumeroDocumento", 20);

        if (limpio.Length < 5 || !limpio.All(char.IsLetterOrDigit))
        {
            throw new ReglaNegocioException(
                "El numero de documento debe tener al menos 5 caracteres alfanumericos.");
        }

        return limpio;
    }

    public static int IdCatalogoValido(int valor, string campo)
    {
        if (valor <= 0)
        {
            throw new ReglaNegocioException($"El campo {campo} debe referenciar un item de catalogo valido.");
        }

        return valor;
    }

    private static string ValidarPresenciaYLongitud(string? valor, string campo, int longitudMaxima)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException($"El campo {campo} es obligatorio.");
        }

        var limpio = valor.Trim();
        if (limpio.Length > longitudMaxima)
        {
            throw new ReglaNegocioException(
                $"El campo {campo} supera la longitud maxima de {longitudMaxima} caracteres.");
        }

        return limpio;
    }
}
