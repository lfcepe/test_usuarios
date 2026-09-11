using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Devsu.Clientes.Application.Clientes.Contratos;
using Devsu.Clientes.Application.Dtos;
using Devsu.Clientes.IntegrationTests.Comun;
using Devsu.Contracts.Eventos;
using FluentAssertions;
using Xunit;

namespace Devsu.Clientes.IntegrationTests;

/// <summary>
/// Prueba de integracion del microservicio de Clientes. Cubre la funcionalidad F6
/// del enunciado.
/// </summary>
/// <remarks>
/// Recorre la pila completa: enrutado HTTP, serializacion, pipeline de MediatR,
/// validadores, entidad de dominio, EF Core y base de datos relacional. Lo unico
/// sustituido es el broker de mensajeria.
/// </remarks>
public sealed class ClientesEndpointsTests : IClassFixture<FabricaAplicacion>
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly FabricaAplicacion _fabrica;
    private readonly HttpClient _cliente;

    public ClientesEndpointsTests(FabricaAplicacion fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    [Fact]
    public async Task CrearYConsultarCliente_DevuelveLosMismosDatos()
    {
        var peticion = NuevoCliente("1712345678", "jose.lema@devsu.com");

        var respuestaAlta = await _cliente.PostAsJsonAsync("/api/clientes", peticion, OpcionesJson);

        respuestaAlta.StatusCode.Should().Be(HttpStatusCode.Created);
        respuestaAlta.Headers.Location.Should().NotBeNull();

        var creado = await respuestaAlta.Content.ReadFromJsonAsync<ClienteDto>(OpcionesJson);
        creado.Should().NotBeNull();
        creado!.Id.Should().BeGreaterThan(0);
        creado.ClienteId.Should().StartWith("CLI-");
        creado.NombreCompleto.Should().Be("JOSE LEMA");
        creado.Estado.Should().BeTrue();

        // La edad la calcula el trigger en PostgreSQL y el DbContext en SQLite.
        // En los dos casos tiene que llegar informada al cliente HTTP.
        creado.Edad.Should().NotBeNull();

        var respuestaConsulta = await _cliente.GetAsync($"/api/clientes/{creado.Id}");
        respuestaConsulta.StatusCode.Should().Be(HttpStatusCode.OK);

        var consultado = await respuestaConsulta.Content.ReadFromJsonAsync<ClienteDto>(OpcionesJson);
        consultado!.NumeroDocumento.Should().Be("1712345678");
        consultado.Email.Should().Be("JOSE.LEMA@DEVSU.COM");
        consultado.TipoDocumento.Should().Be("CEDULA");
        consultado.EstadoDescripcion.Should().Be("ACTIVO");
    }

    [Fact]
    public async Task CrearCliente_EncolaElEventoDeIntegracion()
    {
        _fabrica.Eventos.Limpiar();

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/clientes",
            NuevoCliente("1798765432", "evento.prueba@devsu.com"),
            OpcionesJson);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        _fabrica.Eventos.Eventos
            .OfType<ClienteCreado>()
            .Should().ContainSingle(evento => evento.NumeroDocumento == "1798765432");
    }

    [Fact]
    public async Task CrearCliente_ConDocumentoRepetido_DevuelveConflicto()
    {
        var peticion = NuevoCliente("1755555555", "repetido@devsu.com");

        var primera = await _cliente.PostAsJsonAsync("/api/clientes", peticion, OpcionesJson);
        primera.StatusCode.Should().Be(HttpStatusCode.Created);

        var segunda = await _cliente.PostAsJsonAsync("/api/clientes", peticion, OpcionesJson);

        segunda.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problema = await segunda.Content.ReadFromJsonAsync<JsonElement>();
        problema.GetProperty("codigo").GetString().Should().Be("RECURSO_DUPLICADO");
    }

    [Fact]
    public async Task CrearCliente_ConDatosInvalidos_Devuelve422ConElDetallePorCampo()
    {
        var peticion = NuevoCliente("1766666666", "no-es-un-correo") with
        {
            NumeroCelular = "123",
            PrimerNombre = "",
        };

        var respuesta = await _cliente.PostAsJsonAsync("/api/clientes", peticion, OpcionesJson);

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        problema.GetProperty("codigo").GetString().Should().Be("VALIDACION");
        problema.GetProperty("errors").EnumerateObject().Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task ObtenerCliente_QueNoExiste_DevuelveNotFound()
    {
        var respuesta = await _cliente.GetAsync("/api/clientes/999999");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        problema.GetProperty("codigo").GetString().Should().Be("RECURSO_NO_ENCONTRADO");
    }

    [Fact]
    public async Task CambiarEstado_DesactivaElClienteYSeReflejaEnLaConsulta()
    {
        var alta = await _cliente.PostAsJsonAsync(
            "/api/clientes",
            NuevoCliente("1744444444", "estado@devsu.com"),
            OpcionesJson);

        var creado = await alta.Content.ReadFromJsonAsync<ClienteDto>(OpcionesJson);

        var respuesta = await _cliente.PatchAsJsonAsync(
            $"/api/clientes/{creado!.Id}/estado",
            new CambiarEstadoRequest(false),
            OpcionesJson);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var consultado = await _cliente.GetFromJsonAsync<ClienteDto>(
            $"/api/clientes/{creado.Id}",
            OpcionesJson);

        consultado!.Estado.Should().BeFalse();
        consultado.EstadoDescripcion.Should().Be("INACTIVO");
    }

    [Fact]
    public async Task ObtenerCatalogo_DevuelveLosTiposDeDocumento()
    {
        var catalogos = await _cliente.GetFromJsonAsync<List<CatalogoDto>>(
            "/api/catalogos?raiz=TIPO_DOCUMENTO",
            OpcionesJson);

        catalogos.Should().NotBeNull();
        catalogos!.Should().Contain(item => item.Item == "CEDULA");
    }

    private static CrearClienteRequest NuevoCliente(string documento, string email) =>
        new(
            "Jose",
            null,
            "Lema",
            null,
            IdTipoDocumentoCedula,
            documento,
            IdGeneroMasculino,
            "Otavalo sn y principal",
            "0982547850",
            email,
            new DateOnly(1985, 3, 14),
            "1234");

    private const int IdTipoDocumentoCedula = 2;
    private const int IdGeneroMasculino = 6;
}
