using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Domain.Repositorios;
using Devsu.Clientes.Infrastructure.Comun;
using Devsu.Clientes.Infrastructure.Mensajeria;
using Devsu.Clientes.Infrastructure.Mensajeria.Consumidores;
using Devsu.Clientes.Infrastructure.Persistencia;
using Devsu.Clientes.Infrastructure.Persistencia.Repositorios;
using Devsu.Clientes.Infrastructure.Seguridad;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devsu.Clientes.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra persistencia, seguridad y mensajeria del microservicio.</summary>
    public static IServiceCollection AgregarInfrastructure(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        servicios.AgregarPersistencia(configuracion);
        servicios.AgregarSeguridad();
        servicios.AgregarMensajeria(configuracion);

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

        servicios.AddDbContext<ClientesDbContext>(opciones =>
            opciones.UseNpgsql(cadenaConexion, npgsql =>
            {
                // Reintentos ante errores transitorios: dentro de Docker es habitual
                // que la API arranque antes de que PostgreSQL acepte conexiones.
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            }));

        servicios.AddScoped<IRepositorioCliente, RepositorioCliente>();
        servicios.AddScoped<IRepositorioPersona, RepositorioPersona>();
        servicios.AddScoped<IRepositorioCatalogo, RepositorioCatalogo>();
        servicios.AddScoped<IRepositorioResumenCuentas, RepositorioResumenCuentas>();
        servicios.AddScoped<IRepositorioOutbox, RepositorioOutbox>();
        servicios.AddScoped<IRepositorioIdempotencia, RepositorioIdempotencia>();
        servicios.AddScoped<IUnitOfWork, UnitOfWork>();
        servicios.AddScoped<SeedContrasenias>();

        return servicios;
    }

    private static IServiceCollection AgregarSeguridad(this IServiceCollection servicios)
    {
        servicios.AddSingleton<IProveedorFechaHora, ProveedorFechaHoraSistema>();
        servicios.AddSingleton<IServicioHashContrasenia, ServicioHashContraseniaPbkdf2>();

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
            configurador.AddConsumer<ConsumidorCuentaAperturada>();
            configurador.AddConsumer<ConsumidorCuentaEstadoCambiado>();

            // Prefijo estable para que las colas de este servicio no colisionen con
            // las del microservicio de Cuentas dentro del mismo broker.
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

                    // Reintentos escalonados antes de mandar el mensaje a la cola de
                    // errores. Cubre indisponibilidades cortas de la base de datos.
                    bus.UseMessageRetry(reintento =>
                        reintento.Incremental(3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)));

                    bus.ConfigureEndpoints(contexto);
                });
            }
            else
            {
                // Transporte en memoria para pruebas y para poder arrancar sin broker.
                // Mantiene resoluble IPublishEndpoint sin abrir ninguna conexion.
                configurador.UsingInMemory((contexto, bus) => bus.ConfigureEndpoints(contexto));
            }
        });

        servicios.AddHostedService<PublicadorOutboxHostedService>();

        return servicios;
    }
}
