using Devsu.Cuentas.Domain.Excepciones;

namespace Devsu.Cuentas.Domain.Comun;

/// <summary>
/// Validaciones de invariantes que se ejecutan dentro de las entidades.
/// </summary>
/// <remarks>
/// Ademas de validar, normalizan: todo texto de negocio se almacena en mayusculas
/// y sin espacios sobrantes. Ver <see cref="Normalizador"/>.
/// </remarks>
public static class Guardas
{
    public static string TextoRequerido(string? valor, string campo, int longitudMaxima)
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

    public static string NumeroCuenta(string? valor)
    {
        var limpio = TextoRequerido(valor, "NumeroCuenta", 20);

        if (limpio.Length < 4 || !limpio.All(char.IsDigit))
        {
            throw new ReglaNegocioException(
                "El numero de cuenta debe tener al menos 4 digitos y contener solo numeros.");
        }

        return limpio;
    }

    public static decimal ImporteNoNegativo(decimal valor, string campo)
    {
        if (valor < 0)
        {
            throw new ReglaNegocioException($"El campo {campo} no puede ser negativo.");
        }

        return decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Normaliza un importe a dos decimales conservando el signo.</summary>
    /// <remarks>
    /// Se redondea en el dominio y no solo en la base para que el saldo que se
    /// devuelve al cliente sea exactamente el que quedo almacenado.
    /// </remarks>
    public static decimal Importe(decimal valor) =>
        decimal.Round(valor, 2, MidpointRounding.AwayFromZero);

    public static int IdCatalogoValido(int valor, string campo)
    {
        if (valor <= 0)
        {
            throw new ReglaNegocioException($"El campo {campo} debe referenciar un item de catalogo valido.");
        }

        return valor;
    }
}
