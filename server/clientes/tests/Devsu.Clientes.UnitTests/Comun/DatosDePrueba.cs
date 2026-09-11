using Devsu.Clientes.Domain.Catalogos;
using Devsu.Clientes.Domain.Entidades;

namespace Devsu.Clientes.UnitTests.Comun;

/// <summary>
/// Constructor de datos validos para las pruebas.
/// </summary>
/// <remarks>
/// Cada prueba sobrescribe unicamente el campo que le interesa. Asi, cuando una
/// falla, el motivo es exactamente el valor que esa prueba cambio y no un dato de
/// relleno olvidado.
/// </remarks>
internal static class DatosDePrueba
{
    public static DatosPersonales Personales(
        string primerNombre = "Jose",
        string? segundoNombre = null,
        string primerApellido = "Lema",
        string? segundoApellido = null,
        int idTipoDocumento = CatalogoIds.TipoDocumento.Cedula,
        string numeroDocumento = "1712345678",
        int idGenero = CatalogoIds.Genero.Masculino,
        string direccionDomicilio = "Otavalo sn y principal",
        string numeroCelular = "0982547850",
        string email = "jose.lema@devsu.com",
        DateOnly? fechaNacimiento = null) =>
        new(
            primerNombre,
            segundoNombre,
            primerApellido,
            segundoApellido,
            idTipoDocumento,
            numeroDocumento,
            idGenero,
            direccionDomicilio,
            numeroCelular,
            email,
            fechaNacimiento ?? new DateOnly(1985, 3, 14));

    public static Cliente Cliente(
        DatosPersonales? datos = null,
        string clienteId = "CLI-000001",
        string contraseniaHash = "100000.c2FsdA==.aGFzaA==",
        bool activo = true) =>
        Domain.Entidades.Cliente.Crear(datos ?? Personales(), clienteId, contraseniaHash, activo);
}
