namespace Devsu.Cuentas.Domain.Catalogos;

/// <summary>
/// Identificadores fijos de la tabla "Catalogos".
/// </summary>
/// <remarks>
/// Son los mismos numeros que en la base del microservicio de Clientes, porque
/// viajan dentro de los eventos de integracion. Estan sembrados de forma explicita
/// en BaseDatos.sql y cualquier cambio hay que replicarlo en los dos sitios.
/// </remarks>
public static class CatalogoIds
{
    public static class Raiz
    {
        public const int TipoCuenta = 15;
        public const int EstadoCuenta = 18;
        public const int TipoMovimiento = 21;
        public const int EstadoMovimiento = 24;
    }

    public static class TipoCuenta
    {
        public const int Ahorros = 16;
        public const int Corriente = 17;
    }

    public static class EstadoCuenta
    {
        public const int Activa = 19;
        public const int Inactiva = 20;

        public static int Desde(bool activa) => activa ? Activa : Inactiva;
    }

    public static class TipoMovimiento
    {
        public const int Deposito = 22;
        public const int Retiro = 23;

        /// <summary>El signo del valor determina el tipo cuando no se especifica.</summary>
        public static int Desde(decimal valor) => valor < 0 ? Retiro : Deposito;
    }

    public static class EstadoMovimiento
    {
        public const int Aplicado = 25;
        public const int Reversado = 26;
    }

    public static class EstadoCliente
    {
        public const int Activo = 13;
        public const int Inactivo = 14;
    }

    public static readonly IReadOnlyDictionary<string, int> RaicesPorNombre =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["TIPO_CUENTA"] = Raiz.TipoCuenta,
            ["ESTADO_CUENTA"] = Raiz.EstadoCuenta,
            ["TIPO_MOVIMIENTO"] = Raiz.TipoMovimiento,
            ["ESTADO_MOVIMIENTO"] = Raiz.EstadoMovimiento,
        };
}
