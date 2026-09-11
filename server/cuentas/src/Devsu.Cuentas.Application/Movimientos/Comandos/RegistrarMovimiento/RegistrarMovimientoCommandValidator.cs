using FluentValidation;

namespace Devsu.Cuentas.Application.Movimientos.Comandos.RegistrarMovimiento;

public sealed class RegistrarMovimientoCommandValidator : AbstractValidator<RegistrarMovimientoCommand>
{
    public RegistrarMovimientoCommandValidator()
    {
        RuleFor(comando => comando.Datos).NotNull();

        When(comando => comando.Datos is not null, () =>
        {
            RuleFor(comando => comando.Datos)
                .Must(datos => datos.IdCuenta is > 0 || !string.IsNullOrWhiteSpace(datos.NumeroCuenta))
                .WithName("cuenta")
                .WithMessage("Debe indicar idCuenta o numeroCuenta.");

            RuleFor(comando => comando.Datos.Valor)
                .NotEqual(0).WithMessage("El valor del movimiento no puede ser cero.");

            RuleFor(comando => comando.Datos.Descripcion)
                .MaximumLength(256);

            RuleFor(comando => comando.Datos.Fecha)
                .Must(fecha => fecha is null || fecha <= DateTime.UtcNow.AddMinutes(5))
                .WithMessage("La fecha del movimiento no puede estar en el futuro.");
        });
    }
}
