using FluentValidation;

namespace Devsu.Cuentas.Application.Movimientos.Comandos.ActualizarMovimiento;

public sealed class ActualizarMovimientoCommandValidator : AbstractValidator<ActualizarMovimientoCommand>
{
    public ActualizarMovimientoCommandValidator()
    {
        RuleFor(comando => comando.Id).GreaterThan(0);
        RuleFor(comando => comando.Datos).NotNull();

        When(comando => comando.Datos is not null, () =>
        {
            RuleFor(comando => comando.Datos.Valor)
                .NotEqual(0).WithMessage("El valor del movimiento no puede ser cero.");

            RuleFor(comando => comando.Datos.Descripcion).MaximumLength(256);
        });
    }
}
