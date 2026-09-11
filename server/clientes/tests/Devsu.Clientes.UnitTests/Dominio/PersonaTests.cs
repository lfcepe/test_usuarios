using Devsu.Clientes.Domain.Entidades;
using Devsu.Clientes.Domain.Excepciones;
using Devsu.Clientes.UnitTests.Comun;
using FluentAssertions;
using Xunit;

namespace Devsu.Clientes.UnitTests.Dominio;

public sealed class PersonaTests
{
    [Fact]
    public void Crear_ConDatosValidos_DevuelvePersonaActiva()
    {
        var persona = Persona.Crear(DatosDePrueba.Personales());

        persona.Activa.Should().BeTrue();
        persona.NombreCompleto.Should().Be("JOSE LEMA");
    }

    [Theory]
    [InlineData("1985-03-14", "2026-03-13", 40)]
    [InlineData("1985-03-14", "2026-03-14", 41)]
    [InlineData("1985-03-14", "2026-09-10", 41)]
    [InlineData("2000-12-31", "2026-01-01", 25)]
    public void CalcularEdad_EnDistintasFechas_DevuelveAniosCumplidos(
        string nacimiento,
        string hoy,
        int esperada)
    {
        // Replica el comportamiento de fn_calcular_edad en PostgreSQL: anios
        // cumplidos, no anios transcurridos. El dia del cumpleanios ya cuenta.
        var edad = Persona.CalcularEdad(DateOnly.Parse(nacimiento), DateOnly.Parse(hoy));

        edad.Should().Be(esperada);
    }

    [Fact]
    public void CalcularEdad_ConNacimientoPosteriorAHoy_LanzaReglaNegocio()
    {
        var accion = () => Persona.CalcularEdad(new DateOnly(2030, 1, 1), new DateOnly(2026, 1, 1));

        accion.Should().Throw<ReglaNegocioException>();
    }

    [Fact]
    public void AplicarDatosPersonales_RecortaEspaciosYNormalizaAMayusculas()
    {
        var persona = Persona.Crear(DatosDePrueba.Personales(primerNombre: "  Maria   Elena  "));

        // Recorta los extremos y colapsa los espacios internos repetidos.
        persona.PrimerNombre.Should().Be("MARIA ELENA");
    }

    [Fact]
    public void Crear_ConDireccionDemasiadoLarga_LanzaReglaNegocio()
    {
        var direccion = new string('x', 1001);

        var accion = () => Persona.Crear(DatosDePrueba.Personales(direccionDomicilio: direccion));

        accion.Should().Throw<ReglaNegocioException>()
            .WithMessage("*longitud maxima*");
    }

    [Fact]
    public void CambiarEstadoPersona_ADesactivada_ActualizaElCatalogo()
    {
        var persona = Persona.Crear(DatosDePrueba.Personales());

        persona.CambiarEstadoPersona(false);

        persona.Activa.Should().BeFalse();
    }
}
