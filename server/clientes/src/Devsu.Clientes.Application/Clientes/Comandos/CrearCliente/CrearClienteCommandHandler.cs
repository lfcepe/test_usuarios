using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Application.Dtos;
using Devsu.Clientes.Application.Mapeos;
using Devsu.Clientes.Domain.Entidades;
using Devsu.Clientes.Domain.Excepciones;
using Devsu.Clientes.Domain.Repositorios;
using Devsu.Contracts.Eventos;
using MediatR;

namespace Devsu.Clientes.Application.Clientes.Comandos.CrearCliente;

public sealed class CrearClienteCommandHandler : IRequestHandler<CrearClienteCommand, ClienteDto>
{
    private readonly IRepositorioCliente _clientes;
    private readonly IRepositorioResumenCuentas _resumenes;
    private readonly ValidadorCatalogos _validadorCatalogos;
    private readonly IServicioHashContrasenia _hash;
    private readonly IPublicadorEventos _eventos;
    private readonly IProveedorFechaHora _reloj;
    private readonly IUnitOfWork _unidadTrabajo;

    public CrearClienteCommandHandler(
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

    public async Task<ClienteDto> Handle(CrearClienteCommand peticion, CancellationToken cancelacion)
    {
        var datos = peticion.Datos;

        await _validadorCatalogos.ValidarDatosPersonalesAsync(datos.IdTipoDocumento, datos.IdGenero, cancelacion);

        if (await _clientes.ExisteDocumentoAsync(datos.IdTipoDocumento, datos.NumeroDocumento, null, cancelacion))
        {
            throw new RecursoDuplicadoException(
                $"Ya existe una persona registrada con el documento {datos.NumeroDocumento}.");
        }

        Cliente.ValidarContraseniaEnClaro(datos.Contrasenia);

        return await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var consecutivo = await _clientes.ObtenerSiguienteConsecutivoAsync(ct);

            var cliente = Cliente.Crear(
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
                Cliente.FormatearClienteId(consecutivo),
                _hash.Cifrar(datos.Contrasenia),
                datos.Estado);

            await _clientes.AgregarAsync(cliente, ct);

            // Primer guardado: la base asigna el Id y el trigger calcula la edad.
            // El identificador hace falta para poder publicar el evento con el.
            await _unidadTrabajo.GuardarCambiosAsync(ct);

            await _resumenes.AgregarAsync(ResumenCuentasCliente.Crear(cliente.Id, _reloj.AhoraUtc), ct);

            await _eventos.EncolarAsync(
                new ClienteCreado(
                    Guid.NewGuid(),
                    _reloj.AhoraUtc,
                    cliente.Id,
                    cliente.ClienteId,
                    cliente.NombreCompleto,
                    cliente.NumeroDocumento,
                    cliente.IdEstadoCliente,
                    cliente.Activo),
                ct);

            await _unidadTrabajo.GuardarCambiosAsync(ct);

            // Se relee para devolver las descripciones de catalogo y la edad que
            // calculo la base, que la entidad recien creada todavia no tiene.
            var creado = await _clientes.ObtenerPorIdAsync(cliente.Id, ct);
            return creado is null
                ? cliente.ToClienteDto(0)
                : creado.ToClienteDto(0);
        }, cancelacion);
    }
}
