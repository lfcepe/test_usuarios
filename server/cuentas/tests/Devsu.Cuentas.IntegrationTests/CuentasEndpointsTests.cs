using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Devsu.Contracts.Eventos;
using Devsu.Cuentas.Application.Cuentas.Contratos;
using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Movimientos.Contratos;
using Devsu.Cuentas.IntegrationTests.Comun;
using FluentAssertions;
using Xunit;

namespace Devsu.Cuentas.IntegrationTests;

/// <summary>
/// Prueba de integracion del microservicio de Cuentas (funcionalidad F6).
/// </summary>
/// <remarks>
/// Recorre el caso de uso completo del enunciado sobre HTTP real: apertura de
/// cuenta, deposito, retiro que excede el saldo y reporte de estado de cuenta.
/// </remarks>
public sealed class CuentasEndpointsTests : IClassFixture<FabricaAplicacion>
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private const int IdTipoCuentaAhorros = 16;
    private const int IdTipoCuentaCorriente = 17;

    private readonly FabricaAplicacion _fabrica;
    private readonly HttpClient _cliente;

    public CuentasEndpointsTests(FabricaAplicacion fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    [Fact]
    public async Task CrearCuenta_DevuelveCreadaYEncolaElEvento()
    {
        _fabrica.Eventos.Limpiar();

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/cuentas",
            new CrearCuentaRequest(1, "478758", IdTipoCuentaAhorros, 2000m),
            OpcionesJson);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        var creada = await respuesta.Content.ReadFromJsonAsync<CuentaDto>(OpcionesJson);
        creada!.NumeroCuenta.Should().Be("478758");
        creada.SaldoInicial.Should().Be(2000m);
        creada.SaldoDisponible.Should().Be(2000m);
        creada.TipoCuenta.Should().Be("AHORROS");
        creada.Cliente.Should().Be("JOSE LEMA");

        _fabrica.Eventos.Eventos
            .OfType<CuentaAperturada>()
            .Should().ContainSingle(evento => evento.NumeroCuenta == "478758");
    }

    [Fact]
    public async Task CrearCuenta_ConNumeroRepetido_DevuelveConflicto()
    {
        var peticion = new CrearCuentaRequest(1, "111222", IdTipoCuentaAhorros, 50m);

        (await _cliente.PostAsJsonAsync("/api/cuentas", peticion, OpcionesJson))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var segunda = await _cliente.PostAsJsonAsync("/api/cuentas", peticion, OpcionesJson);

        segunda.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CrearCuenta_ConClienteInexistente_DevuelveConflictoNoSincronizado()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/cuentas",
            new CrearCuentaRequest(999, "999888", IdTipoCuentaAhorros, 10m),
            OpcionesJson);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        problema.GetProperty("codigo").GetString().Should().Be("CLIENTE_NO_SINCRONIZADO");
    }

    [Fact]
    public async Task RegistrarDeposito_ActualizaElSaldoDisponibleDeLaCuenta()
    {
        var cuenta = await CrearCuentaAsync("225487", IdTipoCuentaCorriente, 100m, idCliente: 2);

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/movimientos",
            new RegistrarMovimientoRequest(null, "225487", 600m, null, "Deposito de 600", null),
            OpcionesJson);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        var movimiento = await respuesta.Content.ReadFromJsonAsync<MovimientoDto>(OpcionesJson);
        movimiento!.Valor.Should().Be(600m);
        movimiento.Saldo.Should().Be(700m);

        // La respuesta del alta tiene que traer las descripciones de catalogo ya
        // resueltas y no solo los identificadores.
        movimiento.TipoMovimiento.Should().Be("DEPOSITO");
        movimiento.EstadoDescripcion.Should().Be("APLICADO");
        movimiento.NumeroCuenta.Should().Be("225487");

        var actualizada = await _cliente.GetFromJsonAsync<CuentaDto>(
            $"/api/cuentas/{cuenta.Id}",
            OpcionesJson);

        actualizada!.SaldoDisponible.Should().Be(700m);
        actualizada.SaldoInicial.Should().Be(100m, "el saldo de apertura no cambia nunca");
    }

    [Fact]
    public async Task RegistrarRetiro_QueExcedeElSaldo_DevuelveSaldoNoDisponible()
    {
        await CrearCuentaAsync("495878", IdTipoCuentaAhorros, 0m, idCliente: 1);

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/movimientos",
            new RegistrarMovimientoRequest(null, "495878", -150m, null, "Retiro sin fondos", null),
            OpcionesJson);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        // Es el texto literal que exige la funcionalidad F3.
        problema.GetProperty("title").GetString().Should().Be("Saldo no disponible");
        problema.GetProperty("codigo").GetString().Should().Be("SALDO_NO_DISPONIBLE");
        problema.GetProperty("numeroCuenta").GetString().Should().Be("495878");
        problema.GetProperty("detail").GetString().Should().Contain("no cuenta con saldo suficiente");
    }

    [Fact]
    public async Task RegistrarRetiro_RechazadoPorSaldo_NoAlteraLaCuenta()
    {
        var cuenta = await CrearCuentaAsync("333444", IdTipoCuentaAhorros, 120m, idCliente: 1);

        await _cliente.PostAsJsonAsync(
            "/api/movimientos",
            new RegistrarMovimientoRequest(null, "333444", -500m, null, null, null),
            OpcionesJson);

        var sinCambios = await _cliente.GetFromJsonAsync<CuentaDto>(
            $"/api/cuentas/{cuenta.Id}",
            OpcionesJson);

        sinCambios!.SaldoDisponible.Should().Be(120m);
    }

    [Fact]
    public async Task RegistrarMovimiento_ConValorCero_DevuelveErrorDeValidacion()
    {
        await CrearCuentaAsync("555666", IdTipoCuentaAhorros, 100m, idCliente: 1);

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/movimientos",
            new RegistrarMovimientoRequest(null, "555666", 0m, null, null, null),
            OpcionesJson);

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ActualizarMovimiento_RecalculaLaCadenaDeSaldos()
    {
        await CrearCuentaAsync("777888", IdTipoCuentaAhorros, 1000m, idCliente: 1);

        var primero = await RegistrarAsync("777888", 500m);
        var segundo = await RegistrarAsync("777888", -200m);

        var respuesta = await _cliente.PutAsJsonAsync(
            $"/api/movimientos/{primero.Id}",
            new ActualizarMovimientoRequest(300m, "Correccion"),
            OpcionesJson);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var corregido = await respuesta.Content.ReadFromJsonAsync<MovimientoDto>(OpcionesJson);
        corregido!.Saldo.Should().Be(1300m);
        corregido.TipoMovimiento.Should().Be("DEPOSITO");

        var posterior = await _cliente.GetFromJsonAsync<MovimientoDto>(
            $"/api/movimientos/{segundo.Id}",
            OpcionesJson);

        posterior!.Saldo.Should().Be(1100m, "el saldo del movimiento posterior se recalcula");
    }

    [Fact]
    public async Task Reporte_DevuelveElJsonPlanoConLasClavesDelEnunciado()
    {
        await CrearCuentaAsync("496825", IdTipoCuentaAhorros, 540m, idCliente: 2);
        await RegistrarAsync("496825", -540m);

        var respuesta = await _cliente.GetAsync("/api/reportes?cliente=2");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var filas = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        filas.ValueKind.Should().Be(JsonValueKind.Array);

        var fila = filas.EnumerateArray()
            .FirstOrDefault(item => item.GetProperty("Numero Cuenta").GetString() == "496825");

        fila.ValueKind.Should().Be(JsonValueKind.Object);
        fila.GetProperty("Cliente").GetString().Should().Be("MARIANELA MONTALVO");
        fila.GetProperty("Tipo").GetString().Should().Be("AHORROS");
        fila.GetProperty("Saldo Inicial").GetDecimal().Should().Be(540m);
        fila.GetProperty("Estado").GetBoolean().Should().BeTrue();
        fila.GetProperty("Movimiento").GetDecimal().Should().Be(-540m);
        fila.GetProperty("Saldo Disponible").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task ReporteEstadoCuenta_AgrupaPorCuentaYCalculaTotales()
    {
        await CrearCuentaAsync("585545", IdTipoCuentaCorriente, 1000m, idCliente: 1);
        await RegistrarAsync("585545", 250m);
        await RegistrarAsync("585545", -100m);

        var reporte = await _cliente.GetFromJsonAsync<ReporteEstadoCuentaDto>(
            "/api/reportes/estado-cuenta?clienteId=CLI-000001",
            OpcionesJson);

        reporte.Should().NotBeNull();
        reporte!.Cliente.Should().Be("JOSE LEMA");

        var cuenta = reporte.Cuentas.Single(item => item.NumeroCuenta == "585545");
        cuenta.TotalCreditos.Should().Be(250m);
        cuenta.TotalDebitos.Should().Be(-100m);
        cuenta.SaldoDisponible.Should().Be(1150m);
        cuenta.Movimientos.Should().HaveCount(2);
    }

    [Fact]
    public async Task Reporte_ConClienteInexistente_DevuelveNotFound()
    {
        var respuesta = await _cliente.GetAsync("/api/reportes?cliente=987654");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<CuentaDto> CrearCuentaAsync(
        string numeroCuenta,
        int idTipoCuenta,
        decimal saldoInicial,
        int idCliente)
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/cuentas",
            new CrearCuentaRequest(idCliente, numeroCuenta, idTipoCuenta, saldoInicial),
            OpcionesJson);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await respuesta.Content.ReadFromJsonAsync<CuentaDto>(OpcionesJson))!;
    }

    private async Task<MovimientoDto> RegistrarAsync(string numeroCuenta, decimal valor)
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/movimientos",
            new RegistrarMovimientoRequest(null, numeroCuenta, valor, null, null, null),
            OpcionesJson);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await respuesta.Content.ReadFromJsonAsync<MovimientoDto>(OpcionesJson))!;
    }
}
