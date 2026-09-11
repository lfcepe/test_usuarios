using Microsoft.OpenApi.Models;

namespace Devsu.Cuentas.Api.Configuracion;

public static class ConfiguracionSwagger
{
    public static IServiceCollection AgregarSwagger(this IServiceCollection servicios)
    {
        servicios.AddEndpointsApiExplorer();

        servicios.AddSwaggerGen(opciones =>
        {
            opciones.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Devsu - Microservicio de Cuentas",
                Version = "v1",
                Description =
                    "Gestion de cuentas y movimientos. Consume los eventos del microservicio de "
                    + "Clientes a traves de RabbitMQ y mantiene una replica local del titular.",
            });

            var archivoXml = Path.Combine(AppContext.BaseDirectory, "Devsu.Cuentas.Api.xml");
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
