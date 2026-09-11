using FluentValidation;
using MediatR;

namespace Devsu.Cuentas.Application.Comportamientos;

/// <summary>
/// Ejecuta los validadores registrados para la peticion antes de llegar al
/// manejador. Si alguno falla, lanza una unica excepcion con todos los errores.
/// </summary>
/// <remarks>
/// Se agrupan todos los fallos en lugar de cortar en el primero para que el
/// consumidor del API pueda corregir el formulario completo de una sola vez.
/// </remarks>
public sealed class ValidacionBehavior<TPeticion, TRespuesta> : IPipelineBehavior<TPeticion, TRespuesta>
    where TPeticion : notnull
{
    private readonly IEnumerable<IValidator<TPeticion>> _validadores;

    public ValidacionBehavior(IEnumerable<IValidator<TPeticion>> validadores)
    {
        _validadores = validadores;
    }

    public async Task<TRespuesta> Handle(
        TPeticion peticion,
        RequestHandlerDelegate<TRespuesta> siguiente,
        CancellationToken cancelacion)
    {
        if (!_validadores.Any())
        {
            return await siguiente();
        }

        var contexto = new ValidationContext<TPeticion>(peticion);
        var resultados = await Task.WhenAll(
            _validadores.Select(validador => validador.ValidateAsync(contexto, cancelacion)));

        var errores = resultados
            .Where(resultado => !resultado.IsValid)
            .SelectMany(resultado => resultado.Errors)
            .ToList();

        if (errores.Count > 0)
        {
            throw new ValidationException(errores);
        }

        return await siguiente();
    }
}
