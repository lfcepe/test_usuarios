using Devsu.Clientes.Domain.Entidades;

namespace Devsu.Clientes.Domain.Repositorios;

/// <summary>
/// Acceso a la raiz de agregado Cliente.
/// </summary>
/// <remarks>
/// Devuelve entidades de dominio, nunca IQueryable ni DTOs. Exponer IQueryable
/// dejaria escapar detalles del ORM a la capa de aplicacion y haria imposible
/// sustituir el repositorio en las pruebas sin levantar un proveedor real.
/// </remarks>
public interface IRepositorioCliente
{
    Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken cancelacion);

    Task<Cliente?> ObtenerPorClienteIdAsync(string clienteId, CancellationToken cancelacion);

    /// <summary>Carga el cliente con seguimiento de cambios, para modificarlo.</summary>
    /// <remarks>
    /// Las consultas usan AsNoTracking por rendimiento; los comandos necesitan la
    /// entidad seguida por el contexto para que EF detecte las modificaciones.
    /// </remarks>
    Task<Cliente?> ObtenerParaEdicionAsync(int id, CancellationToken cancelacion);

    Task<Cliente?> ObtenerPorDocumentoAsync(int idTipoDocumento, string numeroDocumento, CancellationToken cancelacion);

    /// <summary>Busqueda paginada por nombres, apellidos o numero de documento.</summary>
    Task<(IReadOnlyList<Cliente> Items, int Total)> BuscarAsync(
        string? busqueda,
        bool? activo,
        int pagina,
        int tamanio,
        CancellationToken cancelacion);

    /// <summary>Comprueba la clave natural, opcionalmente excluyendo un cliente ya existente.</summary>
    Task<bool> ExisteDocumentoAsync(
        int idTipoDocumento,
        string numeroDocumento,
        int? idExcluido,
        CancellationToken cancelacion);

    /// <summary>Reserva el siguiente consecutivo para construir el ClienteId.</summary>
    Task<int> ObtenerSiguienteConsecutivoAsync(CancellationToken cancelacion);

    /// <summary>Clientes cuya contrasenia sigue siendo el marcador de datos-prueba.sql.</summary>
    Task<IReadOnlyList<Cliente>> ObtenerConContraseniaPendienteAsync(CancellationToken cancelacion);

    Task AgregarAsync(Cliente cliente, CancellationToken cancelacion);

    void Eliminar(Cliente cliente);
}
