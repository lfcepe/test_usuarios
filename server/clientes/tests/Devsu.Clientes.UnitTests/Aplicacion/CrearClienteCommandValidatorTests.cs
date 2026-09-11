using Devsu.Clientes.Application.Clientes.Comandos.CrearCliente;
using Devsu.Clientes.Application.Clientes.Contratos;
using Devsu.Clientes.Domain.Catalogos;
using FluentAssertions;
using Xunit;

namespace Devsu.Clientes.UnitTests.Aplicacion;

public sealed class CrearClienteCommandValidatorTests
{
    private readonly CrearClienteCommandValidator _validador = new();

    [Fact]
    public void Validate_ConPeticionCorrecta_NoDevuelveErrores()
    {
        var resultado = _validador.Validate(Comando());

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ConCelularDeNueveDigitos_DevuelveError()
    {
        var resultado = _validador.Validate(Comando(celular: "098254785"));

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(error => error.ErrorMessage.Contains("10 digitos"));
    }

    [Fact]
    public void Validate_ConVariosCamposInvalidos_DevuelveTodosLosErroresDeUnaVez()
    {
        // El comportamiento importa: el formulario del frontend se corrige entero
        // en un solo intento en lugar de campo por campo.
        var resultado = _validador.Validate(Comando(celular: "1", email: "no-es-correo", contrasenia: "1"));

        resultado.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void Validate_ConFechaNacimientoFutura_DevuelveError()
    {
        var manana = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        var resultado = _validador.Validate(Comando(fechaNacimiento: manana));

        resultado.IsValid.Should().BeFalse();
    }

    private static CrearClienteCommand Comando(
        string celular = "0982547850",
        string email = "jose.lema@devsu.com",
        string contrasenia = "1234",
        DateOnly? fechaNacimiento = null) =>
        new(new CrearClienteRequest(
            "Jose",
            null,
            "Lema",
            null,
            CatalogoIds.TipoDocumento.Cedula,
            "1712345678",
            CatalogoIds.Genero.Masculino,
            "Otavalo sn y principal",
            celular,
            email,
            fechaNacimiento ?? new DateOnly(1985, 3, 14),
            contrasenia));
}
