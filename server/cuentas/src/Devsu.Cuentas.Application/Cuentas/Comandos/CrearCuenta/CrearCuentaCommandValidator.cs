using FluentValidation;

namespace Devsu.Cuentas.Application.Cuentas.Comandos.CrearCuenta;

public sealed class CrearCuentaCommandValidator : AbstractValidator<CrearCuentaCommand>
{
    public CrearCuentaCommandValidator()
    {
        RuleFor(comando => comando.Datos).NotNull();

        When(comando => comando.Datos is not null, () =>
        {
            RuleFor(comando => comando.Datos.IdCliente)
                .GreaterThan(0).WithMessage("Debe indicar el cliente titular de la cuenta.");

            RuleFor(comando => comando.Datos.NumeroCuenta)
                .NotEmpty().WithMessage("El numero de cuenta es obligatorio.")
                .MinimumLength(4)
                .MaximumLength(20)
                .Matches("^[0-9]+$").WithMessage("El numero de cuenta solo admite digitos.");

            RuleFor(comando => comando.Datos.IdTipoCuenta)
                .GreaterThan(0).WithMessage("Debe indicar el tipo de cuenta.");

            RuleFor(comando => comando.Datos.SaldoInicial)
                .GreaterThanOrEqualTo(0).WithMessage("El saldo inicial no puede ser negativo.");
        });
    }
}
