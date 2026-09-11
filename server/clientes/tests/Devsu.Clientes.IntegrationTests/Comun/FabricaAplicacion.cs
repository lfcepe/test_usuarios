using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Infrastructure.Comun;
using Devsu.Clientes.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devsu.Clientes.IntegrationTests.Comun;

/// <summary>
/// Levanta la API completa en memoria contra SQLite.
/// </summary>
/// <remarks>
/// Se eligio SQLite en memoria y no el proveedor InMemory de EF porque SQLite es
/// relacional de verdad: respeta claves foraneas, restricciones unicas y
/// transacciones, que es justo lo que estas pruebas necesitan ejercitar. Lo que
/// no tiene son los triggers de PostgreSQL, y por eso el DbContext calcula la
/// edad en codigo cuando el proveedor no es Npgsql.
///
/// La conexion se abre una sola vez y se mantiene abierta durante toda la clase:
/// una base SQLite en memoria vive mientras exista al menos una conexion abierta.
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
                // La cadena real no se usa: el DbContext se reemplaza mas abajo.
                // Aun asi tiene que existir, porque la capa de infraestructura
                // falla de forma explicita si falta.
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=pruebas;Username=devsu;Password=devsu",
                ["Mensajeria:Habilitado"] = "false",
                ["Firebase:Habilitado"] = "false",
            });
        });

        constructor.ConfigureServices(servicios =>
        {
            QuitarRegistroDbContext<ClientesDbContext>(servicios);
            servicios.AddDbContext<ClientesDbContext>(opciones => opciones.UseSqlite(_conexion));

            var registroPublicador = servicios.SingleOrDefault(
                servicio => servicio.ServiceType == typeof(IPublicadorEventos));

            if (registroPublicador is not null)
            {
                servicios.Remove(registroPublicador);
            }

            servicios.AddSingleton<IPublicadorEventos>(Eventos);
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

    /// <summary>Crea las tablas y siembra los catalogos minimos.</summary>
    /// <remarks>
    /// EnsureCreated construye el esquema a partir del modelo de EF, no ejecuta
    /// BaseDatos.sql. Los catalogos se insertan con SQL directo porque la entidad
    /// Catalogo no expone constructores publicos: es de solo lectura para el
    /// dominio y no tiene sentido abrirla solo para las pruebas.
    /// </remarks>
    private void CrearEsquema()
    {
        var opciones = new DbContextOptionsBuilder<ClientesDbContext>()
            .UseSqlite(_conexion)
            .Options;

        using var contexto = new ClientesDbContext(opciones, new ProveedorFechaHoraSistema());

        contexto.Database.EnsureCreated();

        contexto.Database.ExecuteSqlRaw(
            """
            INSERT INTO "Catalogos" ("Id", "DetalleCatalogo", "Item", "IdRaiz") VALUES
                (1, 'TIPO_DOCUMENTO', NULL, NULL),
                (2, NULL, 'CEDULA', 1),
                (3, NULL, 'PASAPORTE', 1),
                (4, NULL, 'RUC', 1),
                (5, 'GENERO', NULL, NULL),
                (6, NULL, 'MASCULINO', 5),
                (7, NULL, 'FEMENINO', 5),
                (8, NULL, 'OTRO', 5),
                (9, 'ESTADO_PERSONA', NULL, NULL),
                (10, NULL, 'ACTIVO', 9),
                (11, NULL, 'INACTIVO', 9),
                (12, 'ESTADO_CLIENTE', NULL, NULL),
                (13, NULL, 'ACTIVO', 12),
                (14, NULL, 'INACTIVO', 12);
            """);
    }
}
