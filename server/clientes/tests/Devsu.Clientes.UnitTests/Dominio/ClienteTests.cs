using Devsu.Clientes.Domain.Catalogos;
using Devsu.Clientes.Domain.Entidades;
using Devsu.Clientes.Domain.Excepciones;
using Devsu.Clientes.UnitTests.Comun;
using FluentAssertions;
using Xunit;

namespace Devsu.Clientes.UnitTests.Dominio;

/// <summary>
/// Pruebas de la entidad de dominio Cliente. Corresponden a la funcionalidad F5
/// del enunciado.
/// </summary>
/// <remarks>
/// No hay dobles ni contexto de datos: la entidad protege sus invariantes por si
/// misma y eso es justamente lo que se comprueba aqui.
/// </remarks>
public sealed class ClienteTests
{
    [Fact]
    public void Crear_ConDatosValidos_DevuelveClienteActivo()
    {
        var cliente = DatosDePrueba.Cliente();

        cliente.ClienteId.Should().Be("CLI-000001");
        cliente.Activo.Should().BeTrue();
        cliente.IdEstadoCliente.Should().Be(CatalogoIds.EstadoCliente.Activo);
        cliente.IdEstadoPersona.Should().Be(CatalogoIds.EstadoPersona.Activo);
    }

    [Fact]
    public void Crear_ConDatosValidos_ElClienteEsUnaPersona()
    {
        var cliente = DatosDePrueba.Cliente();

        // El enunciado exige herencia, no composicion. Si alguien cambiara Cliente
        // por una clase con una propiedad Persona dentro, esta prueba lo detecta.
        cliente.Should().BeAssignableTo<Persona>();
        cliente.NumeroDocumento.Should().Be("1712345678");
    }

    [Fact]
    public void Crear_ComoInactivo_DejaClienteYPersonaInactivos()
    {
        var cliente = DatosDePrueba.Cliente(activo: false);

        cliente.Activo.Should().BeFalse();
        cliente.IdEstadoCliente.Should().Be(CatalogoIds.EstadoCliente.Inactivo);
        cliente.IdEstadoPersona.Should().Be(CatalogoIds.EstadoPersona.Inactivo);
    }

    [Fact]
    public void NombreCompleto_SinSegundoNombreNiApellido_OmiteLosAusentes()
    {
        var cliente = DatosDePrueba.Cliente();

        cliente.NombreCompleto.Should().Be("JOSE LEMA");
    }

    [Fact]
    public void NombreCompleto_ConTodosLosNombres_LosConcatenaEnOrden()
    {
        var cliente = DatosDePrueba.Cliente(
            DatosDePrueba.Personales(
                primerNombre: "Maria",
                segundoNombre: "Elena",
                primerApellido: "Montalvo",
                segundoApellido: "Ruiz"));

        cliente.NombreCompleto.Should().Be("MARIA ELENA MONTALVO RUIZ");
    }

    [Theory]
    [InlineData(1, "CLI-000001")]
    [InlineData(42, "CLI-000042")]
    [InlineData(999999, "CLI-999999")]
    public void FormatearClienteId_ConConsecutivoValido_RellenaConCerosALaIzquierda(
        int consecutivo,
        string esperado)
    {
        Cliente.FormatearClienteId(consecutivo).Should().Be(esperado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void FormatearClienteId_ConConsecutivoNoPositivo_LanzaReglaNegocio(int consecutivo)
    {
        var accion = () => Cliente.FormatearClienteId(consecutivo);

        accion.Should().Throw<ReglaNegocioException>();
    }

    [Fact]
    public void CambiarEstado_AInactivo_DesactivaTambienLaPersona()
    {
        var cliente = DatosDePrueba.Cliente();

        cliente.CambiarEstado(false);

        cliente.Activo.Should().BeFalse();
        cliente.Activa.Should().BeFalse();
        cliente.IdEstadoPersona.Should().Be(CatalogoIds.EstadoPersona.Inactivo);
    }

    [Fact]
    public void CambiarEstado_ReactivandoCliente_LoVuelveAPonerActivo()
    {
        var cliente = DatosDePrueba.Cliente(activo: false);

        cliente.CambiarEstado(true);

        cliente.Activo.Should().BeTrue();
        cliente.IdEstadoCliente.Should().Be(CatalogoIds.EstadoCliente.Activo);
    }

    [Fact]
    public void CambiarContrasenia_ConHashValido_ReemplazaElAnterior()
    {
        var cliente = DatosDePrueba.Cliente();
        const string nuevoHash = "100000.bnVldm9TYWx0.bnVldm9IYXNo";

        cliente.CambiarContrasenia(nuevoHash);

        cliente.Contrasenia.Should().Be(nuevoHash);
    }

    [Fact]
    public void CambiarContrasenia_ConHashVacio_LanzaReglaNegocio()
    {
        var cliente = DatosDePrueba.Cliente();

        var accion = () => cliente.CambiarContrasenia("   ");

        accion.Should().Throw<ReglaNegocioException>();
    }

    [Fact]
    public void Actualizar_ConNuevosDatos_LosAplicaYCambiaElEstado()
    {
        var cliente = DatosDePrueba.Cliente();

        cliente.Actualizar(
            DatosDePrueba.Personales(
                primerNombre: "Juan",
                primerApellido: "Osorio",
                numeroDocumento: "1723456789",
                direccionDomicilio: "13 junio y Equinoccial",
                numeroCelular: "0988745870",
                email: "juan.osorio@devsu.com"),
            activo: false);

        cliente.NombreCompleto.Should().Be("JUAN OSORIO");
        cliente.NumeroDocumento.Should().Be("1723456789");
        cliente.Activo.Should().BeFalse();

        // La identidad no se toca al actualizar datos personales.
        cliente.ClienteId.Should().Be("CLI-000001");
    }

    [Fact]
    public void Crear_ConPrimerNombreVacio_LanzaReglaNegocio()
    {
        var accion = () => DatosDePrueba.Cliente(DatosDePrueba.Personales(primerNombre: "  "));

        accion.Should().Throw<ReglaNegocioException>()
            .WithMessage("*PrimerNombre*");
    }

    [Theory]
    [InlineData("098254785")]
    [InlineData("09825478501")]
    [InlineData("09825478a")]
    public void Crear_ConCelularInvalido_LanzaReglaNegocio(string celular)
    {
        var accion = () => DatosDePrueba.Cliente(DatosDePrueba.Personales(numeroCelular: celular));

        accion.Should().Throw<ReglaNegocioException>()
            .WithMessage("*10 digitos*");
    }

    [Theory]
    [InlineData("jose.lema")]
    [InlineData("jose.lema@")]
    [InlineData("@devsu.com")]
    public void Crear_ConEmailInvalido_LanzaReglaNegocio(string email)
    {
        var accion = () => DatosDePrueba.Cliente(DatosDePrueba.Personales(email: email));

        accion.Should().Throw<ReglaNegocioException>();
    }

    [Fact]
    public void Crear_ConEmailValido_LoNormalizaAMayusculas()
    {
        var cliente = DatosDePrueba.Cliente(DatosDePrueba.Personales(email: "Jose.LEMA@Devsu.com"));

        // Todo el texto de negocio se almacena normalizado, tambien el correo.
        cliente.Email.Should().Be("JOSE.LEMA@DEVSU.COM");
    }

    [Fact]
    public void Crear_ConFechaNacimientoFutura_LanzaReglaNegocio()
    {
        var manana = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        var accion = () => DatosDePrueba.Cliente(DatosDePrueba.Personales(fechaNacimiento: manana));

        accion.Should().Throw<ReglaNegocioException>()
            .WithMessage("*posterior a la fecha actual*");
    }

    [Fact]
    public void Crear_ConDocumentoDemasiadoCorto_LanzaReglaNegocio()
    {
        var accion = () => DatosDePrueba.Cliente(DatosDePrueba.Personales(numeroDocumento: "123"));

        accion.Should().Throw<ReglaNegocioException>();
    }

    [Fact]
    public void Crear_ConClienteIdVacio_LanzaReglaNegocio()
    {
        var accion = () => DatosDePrueba.Cliente(clienteId: "");

        accion.Should().Throw<ReglaNegocioException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    public void ValidarContraseniaEnClaro_ConClaveCorta_LanzaReglaNegocio(string? contrasenia)
    {
        var accion = () => Cliente.ValidarContraseniaEnClaro(contrasenia);

        accion.Should().Throw<ReglaNegocioException>()
            .WithMessage("*al menos 4 caracteres*");
    }

    [Fact]
    public void ValidarContraseniaEnClaro_ConLaClaveDelEnunciado_NoLanza()
    {
        // El enunciado usa claves de cuatro digitos como 1234, de modo que el
        // minimo del dominio tiene que admitirlas.
        var accion = () => Cliente.ValidarContraseniaEnClaro("1234");

        accion.Should().NotThrow();
    }

    [Fact]
    public void RequiereHashInicial_ConElMarcadorDelScript_DevuelveTrue()
    {
        var cliente = DatosDePrueba.Cliente(contraseniaHash: Cliente.ContraseniaPendiente);

        cliente.RequiereHashInicial().Should().BeTrue();
    }

    [Fact]
    public void RequiereHashInicial_ConUnHashReal_DevuelveFalse()
    {
        var cliente = DatosDePrueba.Cliente();

        cliente.RequiereHashInicial().Should().BeFalse();
    }
}
