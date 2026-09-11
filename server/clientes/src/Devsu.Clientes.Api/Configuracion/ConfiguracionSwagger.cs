using Microsoft.OpenApi.Models;

namespace Devsu.Clientes.Api.Configuracion;

public static class ConfiguracionSwagger
{
    public static IServiceCollection AgregarSwagger(this IServiceCollection servicios)
    {
        servicios.AddEndpointsApiExplorer();

        servicios.AddSwaggerGen(opciones =>
        {
            opciones.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Devsu - Microservicio de Clientes",
                Version = "v1",
                Description =
                    "Gestion de personas y clientes. Publica eventos de integracion hacia el "
                    + "microservicio de Cuentas a traves de RabbitMQ.",
            });

            var archivoXml = Path.Combine(AppContext.BaseDirectory, "Devsu.Clientes.Api.xml");
            if (File.Exists(archivoXml))
            {
                opciones.IncludeXmlComments(archivoXml);
            }

            opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Token de Firebase Authentication. Solo se exige si Firebase:Habilitado es true.",
            });

            opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    },
                    Array.Empty<string>()
                },
            });
        });

        return servicios;
    }
}
