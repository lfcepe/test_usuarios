using FluentValidation;

namespace Devsu.Clientes.Application.Clientes.Comandos.ActualizarCliente;

public sealed class ActualizarClienteCommandValidator : AbstractValidator<ActualizarClienteCommand>
{
    public ActualizarClienteCommandValidator()
    {
        RuleFor(comando => comando.Id).GreaterThan(0);
        RuleFor(comando => comando.Datos).NotNull();

        When(comando => comando.Datos is not null, () =>
        {
            RuleFor(comando => comando.Datos.PrimerNombre).NotEmpty().MaximumLength(64);
            RuleFor(comando => comando.Datos.SegundoNombre).MaximumLength(64);
            RuleFor(comando => comando.Datos.PrimerApellido).NotEmpty().MaximumLength(64);
            RuleFor(comando => comando.Datos.SegundoApellido).MaximumLength(64);
            RuleFor(comando => comando.Datos.IdTipoDocumento).GreaterThan(0);
            RuleFor(comando => comando.Datos.NumeroDocumento)
                .NotEmpty().MinimumLength(5).MaximumLength(20)
                .Matches("^[A-Za-z0-9]+$");
            RuleFor(comando => comando.Datos.IdGenero).GreaterThan(0);
            RuleFor(comando => comando.Datos.DireccionDomicilio).NotEmpty().MaximumLength(1000);
            RuleFor(comando => comando.Datos.NumeroCelular)
                .NotEmpty().Matches("^[0-9]{10}$")
                .WithMessage("El numero celular debe tener exactamente 10 digitos.");
            RuleFor(comando => comando.Datos.Email).NotEmpty().EmailAddress().MaximumLength(500);
            RuleFor(comando => comando.Datos.FechaNacimiento)
                .Must(fecha => fecha != default && fecha < DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("La fecha de nacimiento debe ser anterior a hoy.");

            // Solo se valida si viene informada: vacia significa "no cambiar la clave".
            RuleFor(comando => comando.Datos.Contrasenia)
                .MinimumLength(4).MaximumLength(64)
                .When(comando => !string.IsNullOrWhiteSpace(comando.Datos.Contrasenia));
        });
    }
}
