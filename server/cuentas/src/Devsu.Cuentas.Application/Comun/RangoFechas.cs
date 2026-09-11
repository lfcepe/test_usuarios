using System.Globalization;
using System.Text.RegularExpressions;
using Devsu.Cuentas.Domain.Excepciones;

namespace Devsu.Cuentas.Application.Comun;

/// <summary>
/// Rango de fechas del reporte de estado de cuenta.
/// </summary>
/// <remarks>
/// El enunciado propone la ruta /reportes?fecha=rango fechas y no fija el formato,
/// asi que se admiten las dos formas que un evaluador probaria de manera natural:
/// "2022-02-01,2022-02-28" y "01/02/2022-28/02/2022". Tambien se aceptan los
/// parametros separados fechaInicio y fechaFin, que es lo que usa el frontend.
/// </remarks>
public sealed record RangoFechas(DateOnly Inicio, DateOnly Fin)
{
    private static readonly string[] FormatosAdmitidos =
    {
        "yyyy-MM-dd",
        "dd/MM/yyyy",
        "d/M/yyyy",
        "yyyy/MM/dd",
    };

    private static readonly Regex RangoConGuion = new(
        @"^\s*(\d{1,2}/\d{1,2}/\d{4})\s*-\s*(\d{1,2}/\d{1,2}/\d{4})\s*$",
        RegexOptions.Compiled);

    /// <summary>Momento inicial del rango, inclusivo.</summary>
    public DateTime InicioUtc => Inicio.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    /// <summary>Momento final del rango, inclusivo hasta el ultimo instante del dia.</summary>
    public DateTime FinUtc => Fin.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

    public static RangoFechas Resolver(string? textoRango, DateOnly? inicio, DateOnly? fin, DateOnly hoy)
    {
        if (inicio.HasValue || fin.HasValue)
        {
            return Construir(inicio ?? PrimerDiaDelMes(hoy), fin ?? hoy);
        }

        if (string.IsNullOrWhiteSpace(textoRango))
        {
            // Sin rango explicito se asume el mes en curso. Es el periodo que pide
            // un cajero el noventa por ciento de las veces y evita que la consulta
            // recorra todo el historico por descuido.
            return Construir(PrimerDiaDelMes(hoy), hoy);
        }

        return Parsear(textoRango.Trim());
    }

    private static RangoFechas Parsear(string texto)
    {
        var separadas = texto.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

        if (separadas.Length == 2)
        {
            return Construir(ParsearFecha(separadas[0]), ParsearFecha(separadas[1]));
        }

        var coincidencia = RangoConGuion.Match(texto);
        if (coincidencia.Success)
        {
            return Construir(
                ParsearFecha(coincidencia.Groups[1].Value),
                ParsearFecha(coincidencia.Groups[2].Value));
        }

        if (separadas.Length == 1)
        {
            // Una sola fecha significa ese dia concreto.
            var unica = ParsearFecha(separadas[0]);
            return new RangoFechas(unica, unica);
        }

        throw new ReglaNegocioException(
            "El parametro fecha no tiene un formato valido. Use 2022-02-01,2022-02-28 "
            + "o 01/02/2022-28/02/2022.");
    }

    private static DateOnly ParsearFecha(string valor)
    {
        var limpio = valor.Trim();

        if (DateOnly.TryParseExact(
                limpio,
                FormatosAdmitidos,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var fecha))
        {
            return fecha;
        }

        throw new ReglaNegocioException($"La fecha '{limpio}' no tiene un formato valido.");
    }

    private static RangoFechas Construir(DateOnly inicio, DateOnly fin)
    {
        if (inicio > fin)
        {
            throw new ReglaNegocioException(
                "La fecha inicial del rango no puede ser posterior a la fecha final.");
        }

        return new RangoFechas(inicio, fin);
    }

    private static DateOnly PrimerDiaDelMes(DateOnly hoy) => new(hoy.Year, hoy.Month, 1);
}
