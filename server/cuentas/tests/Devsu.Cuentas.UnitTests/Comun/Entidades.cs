using System.Reflection;

namespace Devsu.Cuentas.UnitTests.Comun;

/// <summary>
/// Asigna identificadores a entidades recien construidas.
/// </summary>
/// <remarks>
/// En produccion el identificador lo pone la base de datos y por eso la propiedad
/// no tiene setter publico. Varias pruebas necesitan una cuenta con movimientos ya
/// identificados para ejercitar el recalculo de saldos sin levantar una base, y
/// esta es la via menos invasiva: se escribe el campo de respaldo desde el codigo
/// de pruebas en lugar de abrir la entidad al resto del sistema.
/// </remarks>
internal static class Entidades
{
    public static T ConId<T>(this T entidad, int id)
        where T : notnull
    {
        var campo = BuscarCampoId(entidad.GetType())
            ?? throw new InvalidOperationException(
                $"No se encontro el campo de respaldo de Id en {entidad.GetType().Name}.");

        campo.SetValue(entidad, id);

        return entidad;
    }

    private static FieldInfo? BuscarCampoId(Type? tipo)
    {
        while (tipo is not null)
        {
            var campo = tipo.GetField(
                "<Id>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (campo is not null)
            {
                return campo;
            }

            tipo = tipo.BaseType;
        }

        return null;
    }
}
