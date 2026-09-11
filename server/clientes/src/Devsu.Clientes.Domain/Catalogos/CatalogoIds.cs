namespace Devsu.Clientes.Domain.Catalogos;

/// <summary>
/// Identificadores fijos de la tabla "Catalogos".
/// </summary>
/// <remarks>
/// Estos numeros viajan dentro de los eventos de integracion, asi que los dos
/// microservicios tienen que interpretarlos igual. Estan sembrados de forma
/// explicita en BaseDatos.sql; cualquier cambio hay que hacerlo en los dos sitios.
/// </remarks>
public static class CatalogoIds
{
    public static class Raiz
    {
        public const int TipoDocumento = 1;
        public const int Genero = 5;
        public const int EstadoPersona = 9;
        public const int EstadoCliente = 12;
        public const int TipoCuenta = 15;
        public const int EstadoCuenta = 18;
        public const int TipoMovimiento = 21;
        public const int EstadoMovimiento = 24;
    }

    public static class TipoDocumento
    {
        public const int Cedula = 2;
        public const int Pasaporte = 3;
        public const int Ruc = 4;
    }

    public static class Genero
    {
        public const int Masculino = 6;
        public const int Femenino = 7;
        public const int Otro = 8;
    }

    public static class EstadoPersona
    {
        public const int Activo = 10;
        public const int Inactivo = 11;

        public static int Desde(bool activo) => activo ? Activo : Inactivo;
    }

    public static class EstadoCliente
    {
        public const int Activo = 13;
        public const int Inactivo = 14;

        public static int Desde(bool activo) => activo ? Activo : Inactivo;
    }

    /// <summary>Nombres de las raices tal como se consultan desde /api/catalogos.</summary>
    public static readonly IReadOnlyDictionary<string, int> RaicesPorNombre =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["TIPO_DOCUMENTO"] = Raiz.TipoDocumento,
            ["GENERO"] = Raiz.Genero,
            ["ESTADO_PERSONA"] = Raiz.EstadoPersona,
            ["ESTADO_CLIENTE"] = Raiz.EstadoCliente,
            ["TIPO_CUENTA"] = Raiz.TipoCuenta,
            ["ESTADO_CUENTA"] = Raiz.EstadoCuenta,
            ["TIPO_MOVIMIENTO"] = Raiz.TipoMovimiento,
            ["ESTADO_MOVIMIENTO"] = Raiz.EstadoMovimiento,
        };
}
