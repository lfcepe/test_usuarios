using System.Globalization;
using System.Text.RegularExpressions;

namespace Devsu.Cuentas.Domain.Comun;

/// <summary>
/// Normaliza el texto que se persiste: recorta, colapsa espacios internos y pasa
/// a mayusculas.
/// </summary>
/// <remarks>
/// La regla es la misma que aplica sp_registrar_catalogo en la base de datos, de
/// modo que un valor escrito por la aplicacion y otro cargado por script quedan
/// identicos. Guardar en mayusculas evita que "Jose Lema", "JOSE LEMA" y
/// "jose lema" convivan como si fueran tres personas distintas y hace que las
/// busquedas por nombre no dependan de como escribiera el operador de turno.
///
/// Se usa la cultura invariante a proposito: con la cultura turca, ToUpper('i')
/// devuelve una i con punto y el valor almacenado dejaria de coincidir con el de
/// cualquier otro servidor.
/// </remarks>
public static class Normalizador
{
    private static readonly Regex EspaciosRepetidos = new(@"\s+", RegexOptions.Compiled);

    public static string ATextoNormalizado(string valor) =>
        EspaciosRepetidos
            .Replace(valor.Trim(), " ")
            .ToUpper(CultureInfo.InvariantCulture);

    /// <summary>Limpia espacios sin tocar el uso de mayusculas y minusculas.</summary>
    /// <remarks>
    /// Para valores donde el caso es significativo, como el hash de la contrasenia,
    /// que va en Base64 y cambiaria por completo al pasarlo a mayusculas.
    /// </remarks>
    public static string ATextoLimpio(string valor) => valor.Trim();
}
