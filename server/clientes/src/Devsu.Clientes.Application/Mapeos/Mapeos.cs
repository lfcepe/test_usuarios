using Devsu.Clientes.Application.Dtos;
using Devsu.Clientes.Domain.Entidades;

namespace Devsu.Clientes.Application.Mapeos;

/// <summary>
/// Conversion de entidades a DTOs.
/// </summary>
/// <remarks>
/// Se hace a mano en lugar de con AutoMapper. Son pocas conversiones, el mapeo
/// queda explicito y compila: si manana se renombra una propiedad de la entidad,
/// el error salta al compilar y no en una peticion en produccion.
/// </remarks>
public static class Mapeos
{
    public static ClienteDto ToClienteDto(this Cliente cliente, int totalCuentas) => new(
        cliente.Id,
        cliente.ClienteId,
        cliente.PrimerNombre,
        cliente.SegundoNombre,
        cliente.PrimerApellido,
        cliente.SegundoApellido,
        cliente.NombreCompleto,
        cliente.IdTipoDocumento,
        cliente.TipoDocumento?.Item,
        cliente.NumeroDocumento,
        cliente.IdGenero,
        cliente.Genero?.Item,
        cliente.DireccionDomicilio,
        cliente.NumeroCelular,
        cliente.Email,
        cliente.FechaNacimiento,
        cliente.Edad,
        cliente.Activo,
        cliente.IdEstadoCliente,
        cliente.EstadoCliente?.Item,
        totalCuentas,
        cliente.FechaCreacion,
        cliente.FechaModificacion);

    public static PersonaDto ToPersonaDto(this Persona persona) => new(
        persona.Id,
        persona.PrimerNombre,
        persona.SegundoNombre,
        persona.PrimerApellido,
        persona.SegundoApellido,
        persona.NombreCompleto,
        persona.IdTipoDocumento,
        persona.TipoDocumento?.Item,
        persona.NumeroDocumento,
        persona.IdGenero,
        persona.Genero?.Item,
        persona.DireccionDomicilio,
        persona.NumeroCelular,
        persona.Email,
        persona.FechaNacimiento,
        persona.Edad,
        persona.Activa,
        persona.FechaCreacion,
        persona.FechaModificacion);

    public static CatalogoDto ToCatalogoDto(this Catalogo catalogo) => new(
        catalogo.Id,
        catalogo.DetalleCatalogo,
        catalogo.Item,
        catalogo.IdRaiz);
}
