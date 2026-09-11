using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Domain.Repositorios;
using Devsu.Cuentas.Infrastructure.Comun;
using Devsu.Cuentas.Infrastructure.Mensajeria;
using Devsu.Cuentas.Infrastructure.Mensajeria.Consumidores;
using Devsu.Cuentas.Infrastructure.Persistencia;
using Devsu.Cuentas.Infrastructure.Persistencia.Repositorios;
using Devsu.Cuentas.Infrastructure.ServiciosExternos;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devsu.Cuentas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AgregarInfrastructure(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        servicios.AgregarPersistencia(configuracion);
        servicios.AgregarServiciosExternos(configuracion);
        servicios.AgregarMensajeria(configuracion);

        servicios.AddSingleton<IProveedorFechaHora, ProveedorFechaHoraSistema>();

        return servicios;
    }

    private static IServiceCollection AgregarPersistencia(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var cadenaConexion = configuracion.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexion 'Postgres'. Revise appsettings.json o la variable "
                + "de entorno ConnectionStrings__Postgres.");

        servicios.AddDbContext<CuentasDbContext>(opciones =>
            opciones.UseNpgsql(cadenaConexion, npgsql =>
            {
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            }));

        servicios.AddScoped<IRepositorioCuenta, RepositorioCuenta>();
        servicios.AddScoped<IRepositorioMovimiento, RepositorioMovimiento>();
        servicios.AddScoped<IRepositorioClienteRef, RepositorioClienteRef>();
        servicios.AddScoped<IRepositorioCatalogo, RepositorioCatalogo>();
        servicios.AddScoped<IRepositorioOutbox, RepositorioOutbox>();
        servicios.AddScoped<IRepositorioIdempotencia, RepositorioIdempotencia>();
        servicios.AddScoped<IUnitOfWork, UnitOfWork>();

        return servicios;
    }

    private static IServiceCollection AgregarServiciosExternos(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var direccionBase = configuracion.GetValue<string>("ServiciosExternos:ClientesApi")
            ?? "http://localhost:8081";

        servicios
            .AddHttpClient<IClientesApiClient, ClientesApiClient>(cliente =>
            {
                cliente.BaseAddress = new Uri(direccionBase.TrimEnd('/') + "/");

                // Tiempo limite corto: esta llamada esta dentro de una peticion HTTP
                // que ya tiene a un usuario esperando. Es preferible fallar rapido y
                // devolver un 409 accionable que dejar la peticion colgada.
                cliente.Timeout = TimeSpan.FromSeconds(10);
            })
            // Politica estandar: reintentos con espera exponencial, cortacircuitos
            // que deja de insistir cuando el otro servicio esta caido, y limite de
            // concurrencia para no agotar el pool de conexiones.
            .AddStandardResilienceHandler();

        return servicios;
    }

    private static IServiceCollection AgregarMensajeria(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var seccion = configuracion.GetSection(OpcionesMensajeria.Seccion);
        servicios.Configure<OpcionesMensajeria>(seccion);

        var opciones = seccion.Get<OpcionesMensajeria>() ?? new OpcionesMensajeria();

        servicios.AddScoped<IPublicadorEventos, PublicadorEventosOutbox>();

        servicios.AddMassTransit(configurador =>
        {
            configurador.AddConsumer<ConsumidorClienteCreado>();
            configurador.AddConsumer<ConsumidorClienteActualizado>();
            configurador.AddConsumer<ConsumidorClienteEstadoCambiado>();
            configurador.AddConsumer<ConsumidorClienteEliminado>();

            configurador.SetEndpointNameFormatter(
                new KebabCaseEndpointNameFormatter(opciones.Cola, includeNamespace: false));

            if (opciones.Habilitado)
            {
                configurador.UsingRabbitMq((contexto, bus) =>
                {
                    bus.Host(opciones.Host, opciones.Puerto, opciones.VirtualHost, anfitrion =>
                    {
                        anfitrion.Username(opciones.Usuario);
                        anfitrion.Password(opciones.Contrasenia);
                    });

                    bus.UseMessageRetry(reintento =>
                        reintento.Incremental(3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)));

                    bus.ConfigureEndpoints(contexto);
                });
            }
            else
            {
                configurador.UsingInMemory((contexto, bus) => bus.ConfigureEndpoints(contexto));
            }
        });

        servicios.AddHostedService<PublicadorOutboxHostedService>();

        return servicios;
    }
}
