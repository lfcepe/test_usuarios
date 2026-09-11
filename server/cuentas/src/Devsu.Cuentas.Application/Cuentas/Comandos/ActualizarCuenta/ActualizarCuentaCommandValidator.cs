using FluentValidation;

namespace Devsu.Cuentas.Application.Cuentas.Comandos.ActualizarCuenta;

public sealed class ActualizarCuentaCommandValidator : AbstractValidator<ActualizarCuentaCommand>
{
    public ActualizarCuentaCommandValidator()
    {
        RuleFor(comando => comando.Id).GreaterThan(0);
        RuleFor(comando => comando.Datos).NotNull();
        RuleFor(comando => comando.Datos.IdTipoCuenta)
            .GreaterThan(0)
            .When(comando => comando.Datos is not null);
    }
}
