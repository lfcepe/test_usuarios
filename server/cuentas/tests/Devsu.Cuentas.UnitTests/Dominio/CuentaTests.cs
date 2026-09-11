using Devsu.Cuentas.Domain.Catalogos;
using Devsu.Cuentas.Domain.Excepciones;
using Devsu.Cuentas.UnitTests.Comun;
using FluentAssertions;
using Xunit;

namespace Devsu.Cuentas.UnitTests.Dominio;

/// <summary>
/// Pruebas de la raiz de agregado Cuenta. Cubren las reglas de las funcionalidades
/// F2 (registro de movimientos con actualizacion de saldo) y F3 (saldo no
/// disponible) del enunciado.
/// </summary>
public sealed class CuentaTests
{
    [Fact]
    public void Crear_ConDatosValidos_DejaElSaldoDisponibleIgualAlInicial()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 2000m);

        cuenta.SaldoInicial.Should().Be(2000m);
        cuenta.SaldoDisponible.Should().Be(2000m);
        cuenta.Activa.Should().BeTrue();
        cuenta.Movimientos.Should().BeEmpty();
    }

    [Fact]
    public void Crear_ConSaldoInicialNegativo_LanzaReglaNegocio()
    {
        var accion = () => DatosDePrueba.Cuenta(saldoInicial: -1m);

        accion.Should().Throw<ReglaNegocioException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("47875a")]
    public void Crear_ConNumeroCuentaInvalido_LanzaReglaNegocio(string numeroCuenta)
    {
        var accion = () => DatosDePrueba.Cuenta(numeroCuenta: numeroCuenta);

        accion.Should().Throw<ReglaNegocioException>();
    }

    [Fact]
    public void RegistrarMovimiento_ConDeposito_AumentaElSaldoDisponible()
    {
        var cuenta = DatosDePrueba.Cuenta(numeroCuenta: "225487", saldoInicial: 100m);

        var movimiento = cuenta.RegistrarMovimiento(600m, DatosDePrueba.Momento);

        cuenta.SaldoDisponible.Should().Be(700m);
        movimiento.Valor.Should().Be(600m);
        movimiento.Saldo.Should().Be(700m);
        movimiento.IdTipoMovimiento.Should().Be(CatalogoIds.TipoMovimiento.Deposito);
        movimiento.EsDeposito.Should().BeTrue();
    }

    [Fact]
    public void RegistrarMovimiento_ConRetiro_DisminuyeElSaldoDisponible()
    {
        // Caso de uso 4 del enunciado: cuenta 478758 con 2000 y retiro de 575.
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 2000m);

        var movimiento = cuenta.RegistrarMovimiento(-575m, DatosDePrueba.Momento);

        cuenta.SaldoDisponible.Should().Be(1425m);
        movimiento.Saldo.Should().Be(1425m);
        movimiento.IdTipoMovimiento.Should().Be(CatalogoIds.TipoMovimiento.Retiro);
        movimiento.EsRetiro.Should().BeTrue();
    }

    [Fact]
    public void RegistrarMovimiento_ConRetiroExacto_DejaElSaldoEnCero()
    {
        // Caso de uso 4 del enunciado: cuenta 496825 con 540 y retiro de 540.
        var cuenta = DatosDePrueba.Cuenta(numeroCuenta: "496825", saldoInicial: 540m);

        cuenta.RegistrarMovimiento(-540m, DatosDePrueba.Momento);

        cuenta.SaldoDisponible.Should().Be(0m);
    }

    [Fact]
    public void RegistrarMovimiento_ConRetiroQueExcedeElSaldo_LanzaSaldoNoDisponible()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 2000m);

        var accion = () => cuenta.RegistrarMovimiento(-2000.01m, DatosDePrueba.Momento);

        var excepcion = accion.Should().Throw<SaldoNoDisponibleException>().Which;

        // El enunciado exige exactamente este texto como mensaje de alerta.
        excepcion.Titulo.Should().Be("Saldo no disponible");
        excepcion.Codigo.Should().Be("SALDO_NO_DISPONIBLE");
        excepcion.NumeroCuenta.Should().Be("478758");
        excepcion.SaldoDisponible.Should().Be(2000m);
        excepcion.ValorSolicitado.Should().Be(-2000.01m);
    }

    [Fact]
    public void RegistrarMovimiento_RechazadoPorSaldo_NoDejaRastroEnLaCuenta()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 100m);

        var accion = () => cuenta.RegistrarMovimiento(-500m, DatosDePrueba.Momento);
        accion.Should().Throw<SaldoNoDisponibleException>();

        // La cuenta tiene que quedar exactamente como estaba: ni saldo tocado ni
        // movimiento a medias en la coleccion.
        cuenta.SaldoDisponible.Should().Be(100m);
        cuenta.Movimientos.Should().BeEmpty();
    }

    [Fact]
    public void RegistrarMovimiento_SobreCuentaSinFondos_LanzaSaldoNoDisponible()
    {
        // Caso de uso 4 del enunciado: cuenta 495878 arranca en 0.
        var cuenta = DatosDePrueba.Cuenta(numeroCuenta: "495878", saldoInicial: 0m);

        var accion = () => cuenta.RegistrarMovimiento(-1m, DatosDePrueba.Momento);

        accion.Should().Throw<SaldoNoDisponibleException>();
    }

    [Fact]
    public void RegistrarMovimiento_ConValorCero_LanzaReglaNegocio()
    {
        var cuenta = DatosDePrueba.Cuenta();

        var accion = () => cuenta.RegistrarMovimiento(0m, DatosDePrueba.Momento);

        accion.Should().Throw<ReglaNegocioException>()
            .WithMessage("*no puede ser cero*");
    }

    [Fact]
    public void RegistrarMovimiento_SobreCuentaInactiva_LanzaCuentaInactiva()
    {
        var cuenta = DatosDePrueba.Cuenta(activa: false);

        var accion = () => cuenta.RegistrarMovimiento(100m, DatosDePrueba.Momento);

        accion.Should().Throw<CuentaInactivaException>()
            .Which.Codigo.Should().Be("CUENTA_INACTIVA");
    }

    [Theory]
    [InlineData(150, CatalogoIds.TipoMovimiento.Deposito)]
    [InlineData(-150, CatalogoIds.TipoMovimiento.Retiro)]
    public void RegistrarMovimiento_SinTipoExplicito_LoDeduceDelSigno(decimal valor, int tipoEsperado)
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 1000m);

        var movimiento = cuenta.RegistrarMovimiento(valor, DatosDePrueba.Momento);

        movimiento.IdTipoMovimiento.Should().Be(tipoEsperado);
    }

    [Fact]
    public void RegistrarMovimiento_ConTipoIncoherenteConElSigno_LanzaReglaNegocio()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 1000m);

        var accion = () => cuenta.RegistrarMovimiento(
            -100m,
            DatosDePrueba.Momento,
            CatalogoIds.TipoMovimiento.Deposito);

        accion.Should().Throw<ReglaNegocioException>()
            .WithMessage("*no corresponde con el signo*");
    }

    [Fact]
    public void RegistrarMovimiento_VariosSeguidos_EncadenaLosSaldos()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 1000m);

        var primero = cuenta.RegistrarMovimiento(500m, DatosDePrueba.Momento);
        var segundo = cuenta.RegistrarMovimiento(-200m, DatosDePrueba.Momento.AddHours(1));
        var tercero = cuenta.RegistrarMovimiento(-1300m, DatosDePrueba.Momento.AddHours(2));

        primero.Saldo.Should().Be(1500m);
        segundo.Saldo.Should().Be(1300m);
        tercero.Saldo.Should().Be(0m);
        cuenta.SaldoDisponible.Should().Be(0m);
        cuenta.Movimientos.Should().HaveCount(3);
    }

    [Fact]
    public void RegistrarMovimiento_ConMasDeDosDecimales_RedondeaADosDecimales()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 100m);

        var movimiento = cuenta.RegistrarMovimiento(10.005m, DatosDePrueba.Momento);

        movimiento.Valor.Should().Be(10.01m);
        cuenta.SaldoDisponible.Should().Be(110.01m);
    }

    [Fact]
    public void ActualizarMovimiento_CambiaElValorYRecalculaLosPosteriores()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 1000m);

        var primero = cuenta.RegistrarMovimiento(500m, DatosDePrueba.Momento).ConId(1);
        var segundo = cuenta.RegistrarMovimiento(-200m, DatosDePrueba.Momento.AddHours(1)).ConId(2);

        cuenta.ActualizarMovimiento(1, 300m, "Correccion del deposito");

        primero.Valor.Should().Be(300m);
        primero.Saldo.Should().Be(1300m);
        primero.Descripcion.Should().Be("CORRECCION DEL DEPOSITO");

        // El movimiento posterior no cambia de valor pero si de saldo resultante.
        segundo.Valor.Should().Be(-200m);
        segundo.Saldo.Should().Be(1100m);

        cuenta.SaldoDisponible.Should().Be(1100m);
    }

    [Fact]
    public void ActualizarMovimiento_QueCambiaElSigno_ActualizaTambienElTipo()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 1000m);
        var movimiento = cuenta.RegistrarMovimiento(500m, DatosDePrueba.Momento).ConId(1);

        cuenta.ActualizarMovimiento(1, -500m, null);

        movimiento.IdTipoMovimiento.Should().Be(CatalogoIds.TipoMovimiento.Retiro);
        cuenta.SaldoDisponible.Should().Be(500m);
    }

    [Fact]
    public void ActualizarMovimiento_QueDejariaUnSaldoIntermedioNegativo_LanzaSaldoNoDisponible()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 1000m);

        cuenta.RegistrarMovimiento(500m, DatosDePrueba.Momento).ConId(1);
        cuenta.RegistrarMovimiento(-1400m, DatosDePrueba.Momento.AddHours(1)).ConId(2);

        // Bajar el primer deposito a 100 dejaria el segundo retiro en descubierto,
        // aunque el saldo final volviera a ser positivo mas adelante.
        var accion = () => cuenta.ActualizarMovimiento(1, 100m, null);

        accion.Should().Throw<SaldoNoDisponibleException>();
    }

    [Fact]
    public void ActualizarMovimiento_ConIdInexistente_LanzaRecursoNoEncontrado()
    {
        var cuenta = DatosDePrueba.Cuenta();
        cuenta.RegistrarMovimiento(100m, DatosDePrueba.Momento).ConId(1);

        var accion = () => cuenta.ActualizarMovimiento(99, 50m, null);

        accion.Should().Throw<RecursoNoEncontradoException>();
    }

    [Fact]
    public void ActualizarMovimiento_ConValorCero_LanzaReglaNegocio()
    {
        var cuenta = DatosDePrueba.Cuenta();
        cuenta.RegistrarMovimiento(100m, DatosDePrueba.Momento).ConId(1);

        var accion = () => cuenta.ActualizarMovimiento(1, 0m, null);

        accion.Should().Throw<ReglaNegocioException>();
    }

    [Fact]
    public void CambiarEstado_ADesactivada_ImpideNuevosMovimientos()
    {
        var cuenta = DatosDePrueba.Cuenta();

        cuenta.CambiarEstado(false);

        cuenta.Activa.Should().BeFalse();
        cuenta.IdEstadoCuenta.Should().Be(CatalogoIds.EstadoCuenta.Inactiva);

        var accion = () => cuenta.RegistrarMovimiento(10m, DatosDePrueba.Momento);
        accion.Should().Throw<CuentaInactivaException>();
    }

    [Fact]
    public void Actualizar_CambiaTipoYEstadoSinTocarLosSaldos()
    {
        var cuenta = DatosDePrueba.Cuenta(saldoInicial: 2000m);
        cuenta.RegistrarMovimiento(-575m, DatosDePrueba.Momento);

        cuenta.Actualizar(CatalogoIds.TipoCuenta.Corriente, activa: false);

        cuenta.IdTipoCuenta.Should().Be(CatalogoIds.TipoCuenta.Corriente);
        cuenta.Activa.Should().BeFalse();
        cuenta.SaldoInicial.Should().Be(2000m);
        cuenta.SaldoDisponible.Should().Be(1425m);
    }
}
