using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Movimientos.Comandos.RegistrarMovimiento;
using Devsu.Cuentas.Application.Movimientos.Contratos;
using Devsu.Cuentas.Domain.Catalogos;
using Devsu.Cuentas.Domain.Entidades;
using Devsu.Cuentas.Domain.Excepciones;
using Devsu.Cuentas.Domain.Repositorios;
using Devsu.Cuentas.UnitTests.Comun;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Devsu.Cuentas.UnitTests.Aplicacion;

public sealed class RegistrarMovimientoCommandHandlerTests
{
    private readonly IRepositorioCuenta _cuentas = Substitute.For<IRepositorioCuenta>();
    private readonly IRepositorioMovimiento _movimientos = Substitute.For<IRepositorioMovimiento>();
    private readonly IRepositorioClienteRef _clientes = Substitute.For<IRepositorioClienteRef>();
    private readonly IClientesApiClient _clientesApi = Substitute.For<IClientesApiClient>();
    private readonly IRepositorioCatalogo _catalogos = Substitute.For<IRepositorioCatalogo>();
    private readonly IProveedorFechaHora _reloj = Substitute.For<IProveedorFechaHora>();
    private readonly IUnitOfWork _unidadTrabajo = Substitute.For<IUnitOfWork>();

    private readonly RegistrarMovimientoCommandHandler _manejador;

    public RegistrarMovimientoCommandHandlerTests()
    {
        _catalogos.PerteneceARaizAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _reloj.AhoraUtc.Returns(DatosDePrueba.Momento);
        _reloj.HoyUtc.Returns(DateOnly.FromDateTime(DatosDePrueba.Momento));

        _clientes.ObtenerPorIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ClienteRef.Crear(
                1, "CLI-000001", "Jose Lema", "1712345678",
                CatalogoIds.EstadoCliente.Activo, true, DatosDePrueba.Momento));

        _unidadTrabajo
            .EjecutarEnTransaccionAsync(
                Arg.Any<Func<CancellationToken, Task<MovimientoDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(llamada =>
            {
                var operacion = llamada.Arg<Func<CancellationToken, Task<MovimientoDto>>>();
                return operacion(CancellationToken.None);
            });

        _manejador = new RegistrarMovimientoCommandHandler(
            _cuentas,
            _movimientos,
            new ResolutorCliente(_clientes, _clientesApi, _reloj, NullLogger<ResolutorCliente>.Instance),
            new ValidadorCatalogos(_catalogos),
            _reloj,
            _unidadTrabajo);
    }

    [Fact]
    public async Task Handle_ConDepositoValido_ActualizaElSaldoYGuardaUnaVez()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 100m, numeroCuenta: "225487");
        _cuentas.ObtenerParaMovimientoPorNumeroAsync("225487", Arg.Any<CancellationToken>())
            .Returns(cuenta);

        var resultado = await _manejador.Handle(Comando(numeroCuenta: "225487", valor: 600m), default);

        resultado.Valor.Should().Be(600m);
        resultado.Saldo.Should().Be(700m);
        resultado.NumeroCuenta.Should().Be("225487");
        cuenta.SaldoDisponible.Should().Be(700m);

        await _unidadTrabajo.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConRetiroSinFondos_PropagaSaldoNoDisponibleYNoGuarda()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 100m, numeroCuenta: "225487");
        _cuentas.ObtenerParaMovimientoPorNumeroAsync("225487", Arg.Any<CancellationToken>())
            .Returns(cuenta);

        var accion = async () =>
            await _manejador.Handle(Comando(numeroCuenta: "225487", valor: -500m), default);

        await accion.Should().ThrowAsync<SaldoNoDisponibleException>();

        await _unidadTrabajo.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConCuentaInexistente_LanzaRecursoNoEncontrado()
    {
        _cuentas.ObtenerParaMovimientoAsync(99, Arg.Any<CancellationToken>()).Returns((Cuenta?)null);

        var accion = async () => await _manejador.Handle(Comando(idCuenta: 99), default);

        await accion.Should().ThrowAsync<RecursoNoEncontradoException>();
    }

    [Fact]
    public async Task Handle_ConClienteInactivo_LanzaClienteInactivo()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 1000m);
        _cuentas.ObtenerParaMovimientoAsync(1, Arg.Any<CancellationToken>()).Returns(cuenta);

        var inactivo = ClienteRef.Crear(
            1, "CLI-000001", "Jose Lema", "1712345678",
            CatalogoIds.EstadoCliente.Inactivo, false, DatosDePrueba.Momento);

        _clientes.ObtenerPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(inactivo);

        var accion = async () => await _manejador.Handle(Comando(idCuenta: 1), default);

        await accion.Should().ThrowAsync<ClienteInactivoException>();
    }

    [Fact]
    public async Task Handle_ConClienteAusenteYApiCaida_LanzaClienteNoSincronizado()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 1000m);
        _cuentas.ObtenerParaMovimientoAsync(1, Arg.Any<CancellationToken>()).Returns(cuenta);

        _clientes.ObtenerPorIdAsync(1, Arg.Any<CancellationToken>()).Returns((ClienteRef?)null);
        _clientesApi.ObtenerClienteAsync(1, Arg.Any<CancellationToken>())
            .Returns<Task<ClienteRemoto?>>(_ => throw new HttpRequestException("sin conexion"));

        var accion = async () => await _manejador.Handle(Comando(idCuenta: 1), default);

        // Ante la duda no se registra el movimiento: es preferible un 409 que el
        // cliente puede reintentar a un asiento sobre un titular no verificado.
        await accion.Should().ThrowAsync<ClienteNoSincronizadoException>();
    }

    [Fact]
    public async Task Handle_ConClienteAusentePeroApiDisponible_RecuperaLaReferenciaYContinua()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 1000m);
        _cuentas.ObtenerParaMovimientoAsync(1, Arg.Any<CancellationToken>()).Returns(cuenta);

        _clientes.ObtenerPorIdAsync(1, Arg.Any<CancellationToken>()).Returns((ClienteRef?)null);
        _clientesApi.ObtenerClienteAsync(1, Arg.Any<CancellationToken>())
            .Returns(new ClienteRemoto(
                1, "CLI-000001", "Jose Lema", "1712345678",
                CatalogoIds.EstadoCliente.Activo, true));

        var resultado = await _manejador.Handle(Comando(idCuenta: 1, valor: 250m), default);

        resultado.Saldo.Should().Be(1250m);

        // La referencia recuperada se guarda para no repetir la llamada HTTP.
        await _clientes.Received(1).AgregarAsync(Arg.Any<ClienteRef>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SinCuentaIndicada_LanzaReglaNegocio()
    {
        var accion = async () => await _manejador.Handle(
            new RegistrarMovimientoCommand(
                new RegistrarMovimientoRequest(null, null, 100m, null, null, null)),
            default);

        await accion.Should().ThrowAsync<ReglaNegocioException>();
    }

    private static RegistrarMovimientoCommand Comando(
        int? idCuenta = 1,
        string? numeroCuenta = null,
        decimal valor = 100m) =>
        new(new RegistrarMovimientoRequest(
            numeroCuenta is null ? idCuenta : null,
            numeroCuenta,
            valor,
            null,
            "Movimiento de prueba",
            DatosDePrueba.Momento));
}
