using FluentValidation;

namespace Devsu.Clientes.Application.Clientes.Comandos.CrearCliente;

/// <summary>
/// Validacion de formato del alta de cliente.
/// </summary>
/// <remarks>
/// Cubre lo que se puede comprobar sin tocar la base de datos. La existencia de
/// los catalogos y la unicidad del documento se validan en el manejador, donde si
/// hay acceso a los repositorios.
/// </remarks>
public sealed class CrearClienteCommandValidator : AbstractValidator<CrearClienteCommand>
{
    public CrearClienteCommandValidator()
    {
        RuleFor(comando => comando.Datos).NotNull();

        When(comando => comando.Datos is not null, () =>
        {
            RuleFor(comando => comando.Datos.PrimerNombre)
                .NotEmpty().WithMessage("El primer nombre es obligatorio.")
                .MaximumLength(64);

            RuleFor(comando => comando.Datos.SegundoNombre)
                .MaximumLength(64);

            RuleFor(comando => comando.Datos.PrimerApellido)
                .NotEmpty().WithMessage("El primer apellido es obligatorio.")
                .MaximumLength(64);

            RuleFor(comando => comando.Datos.SegundoApellido)
                .MaximumLength(64);

            RuleFor(comando => comando.Datos.IdTipoDocumento)
                .GreaterThan(0).WithMessage("Debe indicar el tipo de documento.");

            RuleFor(comando => comando.Datos.NumeroDocumento)
                .NotEmpty().WithMessage("El numero de documento es obligatorio.")
                .MinimumLength(5)
                .MaximumLength(20)
                .Matches("^[A-Za-z0-9]+$").WithMessage("El numero de documento solo admite letras y digitos.");

            RuleFor(comando => comando.Datos.IdGenero)
                .GreaterThan(0).WithMessage("Debe indicar el genero.");

            RuleFor(comando => comando.Datos.DireccionDomicilio)
                .NotEmpty().WithMessage("La direccion es obligatoria.")
                .MaximumLength(1000);

            RuleFor(comando => comando.Datos.NumeroCelular)
                .NotEmpty().WithMessage("El numero celular es obligatorio.")
                .Matches("^[0-9]{10}$").WithMessage("El numero celular debe tener exactamente 10 digitos.");

            RuleFor(comando => comando.Datos.Email)
                .NotEmpty().WithMessage("El correo electronico es obligatorio.")
                .EmailAddress().WithMessage("El correo electronico no tiene un formato valido.")
                .MaximumLength(500);

            RuleFor(comando => comando.Datos.FechaNacimiento)
                .Must(fecha => fecha != default).WithMessage("La fecha de nacimiento es obligatoria.")
                .Must(fecha => fecha < DateOnly.FromDateTime(DateTime.UtcNow))
                    .WithMessage("La fecha de nacimiento debe ser anterior a hoy.");

            RuleFor(comando => comando.Datos.Contrasenia)
                .NotEmpty().WithMessage("La contrasenia es obligatoria.")
                .MinimumLength(4).WithMessage("La contrasenia debe tener al menos 4 caracteres.")
                .MaximumLength(64);
        });
    }
}
