using System.Reflection;
using Devsu.Cuentas.Application.Comportamientos;
using Devsu.Cuentas.Application.Comun;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Devsu.Cuentas.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso, sus validadores y el pipeline de MediatR.</summary>
    public static IServiceCollection AgregarApplication(this IServiceCollection servicios)
    {
        var ensamblado = Assembly.GetExecutingAssembly();

        servicios.AddMediatR(configuracion =>
        {
            configuracion.RegisterServicesFromAssembly(ensamblado);
        });

        // El orden importa: primero se registra el log para que tambien mida el
        // tiempo que consume la validacion, y despues la validacion para que el
        // manejador nunca reciba una peticion mal formada.
        servicios.AddTransient(typeof(IPipelineBehavior<,>), typeof(RegistroBehavior<,>));
        servicios.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidacionBehavior<,>));

        servicios.AddValidatorsFromAssembly(ensamblado, includeInternalTypes: true);

        servicios.AddScoped<ValidadorCatalogos>();
        servicios.AddScoped<ResolutorCliente>();

        return servicios;
    }
}
