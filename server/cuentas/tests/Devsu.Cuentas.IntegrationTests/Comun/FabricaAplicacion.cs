using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Infrastructure.Comun;
using Devsu.Cuentas.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devsu.Cuentas.IntegrationTests.Comun;

/// <summary>
/// Levanta la API de Cuentas en memoria contra SQLite.
/// </summary>
/// <remarks>
/// Se sustituyen las dos dependencias externas del servicio: la base de datos y el
/// broker. El cliente HTTP hacia el microservicio de Clientes tambien se reemplaza
/// por un doble, porque en estas pruebas el titular ya esta replicado en
/// "ClientesRef" y no debe salir ninguna llamada de red.
/// </remarks>
public sealed class FabricaAplicacion : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conexion;

    public FabricaAplicacion()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        CrearEsquema();
    }

    public PublicadorEventosEnMemoria Eventos { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        constructor.UseEnvironment("Testing");

        constructor.ConfigureAppConfiguration((_, configuracion) =>
        {
            configuracion.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=pruebas;Username=devsu;Password=devsu",
                ["Mensajeria:Habilitado"] = "false",
                ["Firebase:Habilitado"] = "false",
                ["ServiciosExternos:ClientesApi"] = "http://localhost:9",
            });
        });

        constructor.ConfigureServices(servicios =>
        {
            QuitarRegistroDbContext<CuentasDbContext>(servicios);
            servicios.AddDbContext<CuentasDbContext>(opciones => opciones.UseSqlite(_conexion));

            Reemplazar<IPublicadorEventos>(servicios);
            servicios.AddSingleton<IPublicadorEventos>(Eventos);

            Reemplazar<IClientesApiClient>(servicios);
            servicios.AddSingleton<IClientesApiClient, ClientesApiClientFalso>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _conexion.Dispose();
        }
    }

    private static void Reemplazar<TServicio>(IServiceCollection servicios)
    {
        var registros = servicios
            .Where(servicio => servicio.ServiceType == typeof(TServicio))
            .ToList();

        foreach (var registro in registros)
        {
            servicios.Remove(registro);
        }
    }

    /// <summary>Elimina el registro del DbContext que hizo la capa de infraestructura.</summary>
    /// <remarks>
    /// No basta con quitar DbContextOptions&lt;TContexto&gt;. Desde EF Core 9, AddDbContext
    /// registra ademas un IDbContextOptionsConfiguration&lt;TContexto&gt; que conserva la
    /// llamada a UseNpgsql; si solo se retira el primero, el contexto de pruebas
    /// sigue intentando conectarse a PostgreSQL y las pruebas fallan con un 500
    /// despues de agotar los reintentos de arranque.
    ///
    /// La deteccion se hace por nombre de tipo y no referenciando la interfaz para
    /// que esto no vuelva a romperse si el tipo cambia de ensamblado.
    /// </remarks>
    private static void QuitarRegistroDbContext<TContexto>(IServiceCollection servicios)
        where TContexto : DbContext
    {
        var registros = servicios
            .Where(servicio =>
                servicio.ServiceType == typeof(DbContextOptions<TContexto>)
                || servicio.ServiceType == typeof(DbContextOptions)
                || (servicio.ServiceType.IsGenericType
                    && servicio.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal)
                    && servicio.ServiceType.GetGenericArguments().Contains(typeof(TContexto))))
            .ToList();

        foreach (var registro in registros)
        {
            servicios.Remove(registro);
        }
    }

    /// <summary>Crea el esquema y siembra catalogos y el cliente replicado.</summary>
    private void CrearEsquema()
    {
        var opciones = new DbContextOptionsBuilder<CuentasDbContext>()
            .UseSqlite(_conexion)
            .Options;

        using var contexto = new CuentasDbContext(opciones, new ProveedorFechaHoraSistema());

        contexto.Database.EnsureCreated();

        contexto.Database.ExecuteSqlRaw(
            """
            INSERT INTO "Catalogos" ("Id", "DetalleCatalogo", "Item", "IdRaiz") VALUES
                (15, 'TIPO_CUENTA', NULL, NULL),
                (16, NULL, 'AHORROS', 15),
                (17, NULL, 'CORRIENTE', 15),
                (18, 'ESTADO_CUENTA', NULL, NULL),
                (19, NULL, 'ACTIVA', 18),
                (20, NULL, 'INACTIVA', 18),
                (21, 'TIPO_MOVIMIENTO', NULL, NULL),
                (22, NULL, 'DEPOSITO', 21),
                (23, NULL, 'RETIRO', 21),
                (24, 'ESTADO_MOVIMIENTO', NULL, NULL),
                (25, NULL, 'APLICADO', 24),
                (26, NULL, 'REVERSADO', 24);
            """);

        // Titular ya replicado, que es la situacion normal cuando el evento
        // ClienteCreado llego antes de que se abriera la cuenta.
        contexto.Database.ExecuteSqlRaw(
            """
            INSERT INTO "ClientesRef"
                ("IdCliente", "ClienteId", "NombreCompleto", "NumeroDocumento",
                 "IdEstadoCliente", "Activo", "FechaSincronizacion") VALUES
                (1, 'CLI-000001', 'JOSE LEMA', '1712345678', 13, 1, '2022-02-01 00:00:00'),
                (2, 'CLI-000002', 'MARIANELA MONTALVO', '1709876543', 13, 1, '2022-02-01 00:00:00');
            """);
    }
}

/// <summary>Doble del cliente HTTP: en estas pruebas no debe salir trafico de red.</summary>
internal sealed class ClientesApiClientFalso : IClientesApiClient
{
    public Task<ClienteRemoto?> ObtenerClienteAsync(int idCliente, CancellationToken cancelacion) =>
        Task.FromResult<ClienteRemoto?>(null);
}
