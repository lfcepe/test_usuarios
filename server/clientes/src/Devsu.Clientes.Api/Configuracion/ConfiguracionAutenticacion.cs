using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Devsu.Clientes.Api.Configuracion;

/// <summary>
/// Validacion opcional de los tokens emitidos por Firebase Authentication.
/// </summary>
/// <remarks>
/// Viene desactivada por defecto (Firebase:Habilitado = false) para que la prueba
/// se pueda recorrer con Postman sin montar un proyecto de Firebase. Al activarla,
/// los tokens se validan contra el emisor de Google usando las claves publicas que
/// el propio middleware descarga del documento de descubrimiento.
/// </remarks>
public static class ConfiguracionAutenticacion
{
    public static IServiceCollection AgregarAutenticacionFirebase(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var habilitado = configuracion.GetValue<bool>("Firebase:Habilitado");
        var projectId = configuracion.GetValue<string>("Firebase:ProjectId");

        if (!habilitado)
        {
            return servicios;
        }

        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new InvalidOperationException(
                "Firebase:Habilitado es true pero falta Firebase:ProjectId.");
        }

        servicios
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opciones =>
            {
                opciones.Authority = $"https://securetoken.google.com/{projectId}";
                opciones.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"https://securetoken.google.com/{projectId}",
                    ValidateAudience = true,
                    ValidAudience = projectId,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        servicios.AddAuthorization();

        return servicios;
    }
}
