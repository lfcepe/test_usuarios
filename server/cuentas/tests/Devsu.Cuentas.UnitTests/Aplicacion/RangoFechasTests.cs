using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Domain.Excepciones;
using FluentAssertions;
using Xunit;

namespace Devsu.Cuentas.UnitTests.Aplicacion;

/// <summary>
/// El enunciado propone /reportes?fecha=rango fechas sin fijar el formato, asi que
/// el parser admite varias formas. Estas pruebas fijan cuales.
/// </summary>
public sealed class RangoFechasTests
{
    private static readonly DateOnly Hoy = new(2022, 2, 15);

    [Fact]
    public void Resolver_ConRangoSeparadoPorComa_DevuelveAmbasFechas()
    {
        var rango = RangoFechas.Resolver("2022-02-01,2022-02-28", null, null, Hoy);

        rango.Inicio.Should().Be(new DateOnly(2022, 2, 1));
        rango.Fin.Should().Be(new DateOnly(2022, 2, 28));
    }

    [Fact]
    public void Resolver_ConRangoEnFormatoLatinoSeparadoPorGuion_DevuelveAmbasFechas()
    {
        var rango = RangoFechas.Resolver("01/02/2022-28/02/2022", null, null, Hoy);

        rango.Inicio.Should().Be(new DateOnly(2022, 2, 1));
        rango.Fin.Should().Be(new DateOnly(2022, 2, 28));
    }

    [Fact]
    public void Resolver_ConUnaSolaFecha_DevuelveEseDiaCompleto()
    {
        var rango = RangoFechas.Resolver("2022-02-10", null, null, Hoy);

        rango.Inicio.Should().Be(new DateOnly(2022, 2, 10));
        rango.Fin.Should().Be(new DateOnly(2022, 2, 10));
    }

    [Fact]
    public void Resolver_SinParametros_TomaElMesEnCurso()
    {
        var rango = RangoFechas.Resolver(null, null, null, Hoy);

        rango.Inicio.Should().Be(new DateOnly(2022, 2, 1));
        rango.Fin.Should().Be(Hoy);
    }

    [Fact]
    public void Resolver_ConFechasExplicitas_TienenPrioridadSobreElTexto()
    {
        var rango = RangoFechas.Resolver(
            "2020-01-01,2020-12-31",
            new DateOnly(2022, 3, 1),
            new DateOnly(2022, 3, 31),
            Hoy);

        rango.Inicio.Should().Be(new DateOnly(2022, 3, 1));
        rango.Fin.Should().Be(new DateOnly(2022, 3, 31));
    }

    [Fact]
    public void Resolver_ConInicioPosteriorAlFin_LanzaReglaNegocio()
    {
        var accion = () => RangoFechas.Resolver("2022-03-01,2022-02-01", null, null, Hoy);

        accion.Should().Throw<ReglaNegocioException>()
            .WithMessage("*no puede ser posterior*");
    }

    [Fact]
    public void Resolver_ConTextoNoParseable_LanzaReglaNegocio()
    {
        var accion = () => RangoFechas.Resolver("el mes pasado", null, null, Hoy);

        accion.Should().Throw<ReglaNegocioException>();
    }

    [Fact]
    public void FinUtc_LlegaHastaElUltimoInstanteDelDia()
    {
        var rango = RangoFechas.Resolver("2022-02-01,2022-02-28", null, null, Hoy);

        // Si el limite superior fuera medianoche, un movimiento de las 15:00 del
        // dia 28 quedaria fuera del reporte sin motivo aparente.
        rango.FinUtc.Date.Should().Be(new DateTime(2022, 2, 28));
        rango.FinUtc.Hour.Should().Be(23);
    }
}
