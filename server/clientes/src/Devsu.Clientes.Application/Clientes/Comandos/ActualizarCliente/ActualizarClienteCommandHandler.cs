using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Application.Dtos;
using Devsu.Clientes.Application.Mapeos;
using Devsu.Clientes.Domain.Entidades;
using Devsu.Clientes.Domain.Excepciones;
using Devsu.Clientes.Domain.Repositorios;
using Devsu.Contracts.Eventos;
using MediatR;

namespace Devsu.Clientes.Application.Clientes.Comandos.ActualizarCliente;

public sealed class ActualizarClienteCommandHandler : IRequestHandler<ActualizarClienteCommand, ClienteDto>
{
    private readonly IRepositorioCliente _clientes;
    private readonly IRepositorioResumenCuentas _resumenes;
    private readonly ValidadorCatalogos _validadorCatalogos;
    private readonly IServicioHashContrasenia _hash;
    private readonly IPublicadorEventos _eventos;
    private readonly IProveedorFechaHora _reloj;
    private readonly IUnitOfWork _unidadTrabajo;

    public ActualizarClienteCommandHandler(
        IRepositorioCliente clientes,
        IRepositorioResumenCuentas resumenes,
        ValidadorCatalogos validadorCatalogos,
        IServicioHashContrasenia hash,
        IPublicadorEventos eventos,
        IProveedorFechaHora reloj,
        IUnitOfWork unidadTrabajo)
    {
        _clientes = clientes;
        _resumenes = resumenes;
        _validadorCatalogos = validadorCatalogos;
        _hash = hash;
        _eventos = eventos;
        _reloj = reloj;
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<ClienteDto> Handle(ActualizarClienteCommand peticion, CancellationToken cancelacion)
    {
        var datos = peticion.Datos;

        var cliente = await _clientes.ObtenerParaEdicionAsync(peticion.Id, cancelacion)
            ?? throw RecursoNoEncontradoException.Cliente(peticion.Id);

        await _validadorCatalogos.ValidarDatosPersonalesAsync(datos.IdTipoDocumento, datos.IdGenero, cancelacion);

        if (await _clientes.ExisteDocumentoAsync(
                datos.IdTipoDocumento, datos.NumeroDocumento, peticion.Id, cancelacion))
        {
            throw new RecursoDuplicadoException($"Otro cliente ya usa el documento {datos.NumeroDocumento}.");
        }

        cliente.Actualizar(
            new DatosPersonales(
                datos.PrimerNombre,
                datos.SegundoNombre,
                datos.PrimerApellido,
                datos.SegundoApellido,
                datos.IdTipoDocumento,
                datos.NumeroDocumento,
                datos.IdGenero,
                datos.DireccionDomicilio,
                datos.NumeroCelular,
                datos.Email,
                datos.FechaNacimiento),
            datos.Estado);

        // Una contrasenia vacia significa "no la cambies", no "dejala en blanco".
        if (!string.IsNullOrWhiteSpace(datos.Contrasenia))
        {
            Cliente.ValidarContraseniaEnClaro(datos.Contrasenia);
            cliente.CambiarContrasenia(_hash.Cifrar(datos.Contrasenia));
        }

        await _eventos.EncolarAsync(
            new ClienteActualizado(
                Guid.NewGuid(),
                _reloj.AhoraUtc,
                cliente.Id,
                cliente.ClienteId,
                cliente.NombreCompleto,
                cliente.NumeroDocumento,
                cliente.IdEstadoCliente,
                cliente.Activo),
            cancelacion);

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        var totales = await _resumenes.ObtenerTotalesAsync(new[] { cliente.Id }, cancelacion);
        var actualizado = await _clientes.ObtenerPorIdAsync(cliente.Id, cancelacion) ?? cliente;

        return actualizado.ToClienteDto(totales.GetValueOrDefault(cliente.Id));
    }
}
