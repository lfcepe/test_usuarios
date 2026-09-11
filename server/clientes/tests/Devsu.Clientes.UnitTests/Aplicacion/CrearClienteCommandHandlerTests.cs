using Devsu.Clientes.Application.Clientes.Comandos.CrearCliente;
using Devsu.Clientes.Application.Clientes.Contratos;
using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Domain.Catalogos;
using Devsu.Clientes.Domain.Entidades;
using Devsu.Clientes.Domain.Excepciones;
using Devsu.Clientes.Domain.Repositorios;
using Devsu.Contracts.Eventos;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Devsu.Clientes.UnitTests.Aplicacion;

/// <summary>
/// Pruebas del caso de uso de alta. Todas las dependencias son dobles: aqui se
/// verifica la orquestacion, no la persistencia.
/// </summary>
public sealed class CrearClienteCommandHandlerTests
{
    private readonly IRepositorioCliente _clientes = Substitute.For<IRepositorioCliente>();
    private readonly IRepositorioResumenCuentas _resumenes = Substitute.For<IRepositorioResumenCuentas>();
    private readonly IRepositorioCatalogo _catalogos = Substitute.For<IRepositorioCatalogo>();
    private readonly IServicioHashContrasenia _hash = Substitute.For<IServicioHashContrasenia>();
    private readonly IPublicadorEventos _eventos = Substitute.For<IPublicadorEventos>();
    private readonly IProveedorFechaHora _reloj = Substitute.For<IProveedorFechaHora>();
    private readonly IUnitOfWork _unidadTrabajo = Substitute.For<IUnitOfWork>();

    private readonly CrearClienteCommandHandler _manejador;

    public CrearClienteCommandHandlerTests()
    {
        _catalogos.PerteneceARaizAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _clientes.ExisteDocumentoAsync(
                Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _clientes.ObtenerSiguienteConsecutivoAsync(Arg.Any<CancellationToken>()).Returns(7);
        _clientes.ObtenerPorIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Cliente?)null);

        _hash.Cifrar(Arg.Any<string>()).Returns("100000.c2FsdA==.aGFzaA==");
        _reloj.AhoraUtc.Returns(new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc));

        // La transaccion se limita a ejecutar la operacion que recibe: lo que se
        // prueba es el contenido del caso de uso, no el comportamiento de EF.
        _unidadTrabajo
            .EjecutarEnTransaccionAsync(
                Arg.Any<Func<CancellationToken, Task<Application.Dtos.ClienteDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(llamada =>
            {
                var operacion = llamada.Arg<Func<CancellationToken, Task<Application.Dtos.ClienteDto>>>();
                return operacion(CancellationToken.None);
            });

        _manejador = new CrearClienteCommandHandler(
            _clientes,
            _resumenes,
            new ValidadorCatalogos(_catalogos),
            _hash,
            _eventos,
            _reloj,
            _unidadTrabajo);
    }

    [Fact]
    public async Task Handle_ConDatosValidos_GuardaElClienteConSuCodigoDeNegocio()
    {
        var resultado = await _manejador.Handle(Comando(), CancellationToken.None);

        await _clientes.Received(1).AgregarAsync(
            Arg.Is<Cliente>(cliente => cliente.ClienteId == "CLI-000007"),
            Arg.Any<CancellationToken>());

        resultado.ClienteId.Should().Be("CLI-000007");
        resultado.NombreCompleto.Should().Be("JOSE LEMA");
    }

    [Fact]
    public async Task Handle_ConDatosValidos_CifraLaContraseniaAntesDeGuardarla()
    {
        await _manejador.Handle(Comando(), CancellationToken.None);

        _hash.Received(1).Cifrar("1234");

        await _clientes.Received(1).AgregarAsync(
            Arg.Is<Cliente>(cliente => cliente.Contrasenia != "1234"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConDatosValidos_EncolaElEventoClienteCreado()
    {
        await _manejador.Handle(Comando(), CancellationToken.None);

        await _eventos.Received(1).EncolarAsync(
            Arg.Is<ClienteCreado>(evento =>
                evento.ClienteId == "CLI-000007" && evento.Activo),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConDatosValidos_CreaElResumenDeCuentasEnCero()
    {
        await _manejador.Handle(Comando(), CancellationToken.None);

        await _resumenes.Received(1).AgregarAsync(
            Arg.Is<ResumenCuentasCliente>(resumen => resumen.TotalCuentas == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConDocumentoYaRegistrado_LanzaRecursoDuplicado()
    {
        _clientes.ExisteDocumentoAsync(
                Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var accion = async () => await _manejador.Handle(Comando(), CancellationToken.None);

        await accion.Should().ThrowAsync<RecursoDuplicadoException>();

        await _clientes.DidNotReceive().AgregarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConTipoDocumentoInexistente_LanzaReglaNegocio()
    {
        _catalogos.PerteneceARaizAsync(
                Arg.Any<int>(), CatalogoIds.Raiz.TipoDocumento, Arg.Any<CancellationToken>())
            .Returns(false);

        var accion = async () => await _manejador.Handle(Comando(), CancellationToken.None);

        await accion.Should().ThrowAsync<ReglaNegocioException>()
            .WithMessage("*tipo de documento*");
    }

    [Fact]
    public async Task Handle_ConContraseniaDemasiadoCorta_LanzaReglaNegocioYNoTocaLaBase()
    {
        var accion = async () =>
            await _manejador.Handle(Comando(contrasenia: "12"), CancellationToken.None);

        await accion.Should().ThrowAsync<ReglaNegocioException>();

        await _unidadTrabajo.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    private static CrearClienteCommand Comando(string contrasenia = "1234") =>
        new(new CrearClienteRequest(
            "Jose",
            null,
            "Lema",
            null,
            CatalogoIds.TipoDocumento.Cedula,
            "1712345678",
            CatalogoIds.Genero.Masculino,
            "Otavalo sn y principal",
            "0982547850",
            "jose.lema@devsu.com",
            new DateOnly(1985, 3, 14),
            contrasenia));
}
