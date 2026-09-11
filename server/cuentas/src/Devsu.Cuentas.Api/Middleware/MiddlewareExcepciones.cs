using System.Diagnostics;
using Devsu.Cuentas.Domain.Excepciones;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Devsu.Cuentas.Api.Middleware;

/// <summary>
/// Traduce cualquier excepcion no controlada a una respuesta ProblemDetails.
/// </summary>
/// <remarks>
/// Aqui es donde se materializa la funcionalidad F3: SaldoNoDisponibleException
/// llega con el titulo "Saldo no disponible", que es el texto que pide el
/// enunciado, y con un detalle que ademas dice cuanto habia y cuanto se pedia.
/// El campo "codigo" permite al frontend distinguir el caso sin leer el mensaje.
/// </remarks>
public sealed class MiddlewareExcepciones
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<MiddlewareExcepciones> _log;
    private readonly IHostEnvironment _entorno;

    public MiddlewareExcepciones(
        RequestDelegate siguiente,
        ILogger<MiddlewareExcepciones> log,
        IHostEnvironment entorno)
    {
        _siguiente = siguiente;
        _log = log;
        _entorno = entorno;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (Exception excepcion)
        {
            await EscribirRespuestaAsync(contexto, excepcion);
        }
    }

    private async Task EscribirRespuestaAsync(HttpContext contexto, Exception excepcion)
    {
        if (contexto.Response.HasStarted)
        {
            _log.LogError(excepcion, "Excepcion despues de haber iniciado la respuesta");
            return;
        }

        var problema = Construir(excepcion, contexto);

        contexto.Response.Clear();
        contexto.Response.StatusCode = problema.Status ?? StatusCodes.Status500InternalServerError;
        contexto.Response.ContentType = "application/problem+json";

        await contexto.Response.WriteAsJsonAsync((object)problema);
    }

    private ProblemDetails Construir(Exception excepcion, HttpContext contexto)
    {
        var traceId = Activity.Current?.Id ?? contexto.TraceIdentifier;

        switch (excepcion)
        {
            case ValidationException validacion:
                _log.LogInformation("Peticion invalida en {Ruta}", contexto.Request.Path);

                var errores = validacion.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        grupo => grupo.Key,
                        grupo => grupo.Select(error => error.ErrorMessage).ToArray());

                var problemaValidacion = new ValidationProblemDetails(errores)
                {
                    Title = "Los datos enviados no son validos",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Detail = "Revise el detalle de los errores por campo.",
                    Instance = contexto.Request.Path,
                };

                problemaValidacion.Extensions["codigo"] = "VALIDACION";
                problemaValidacion.Extensions["traceId"] = traceId;
                return problemaValidacion;

            case SaldoNoDisponibleException saldo:
                _log.LogInformation(
                    "Movimiento rechazado en la cuenta {NumeroCuenta}: saldo {Saldo}, solicitado {Valor}",
                    saldo.NumeroCuenta,
                    saldo.SaldoDisponible,
                    saldo.ValorSolicitado);

                var problemaSaldo = Problema(
                    StatusCodes.Status400BadRequest,
                    saldo.Titulo,
                    saldo.Message,
                    saldo.Codigo,
                    contexto,
                    traceId);

                // Datos estructurados para que el frontend pueda mostrar el detalle
                // sin tener que parsear el texto del mensaje.
                problemaSaldo.Extensions["numeroCuenta"] = saldo.NumeroCuenta;
                problemaSaldo.Extensions["saldoDisponible"] = saldo.SaldoDisponible;
                problemaSaldo.Extensions["valorSolicitado"] = saldo.ValorSolicitado;
                return problemaSaldo;

            case ExcepcionDominio dominio:
                _log.LogInformation(
                    "Regla de dominio incumplida ({Codigo}): {Mensaje}",
                    dominio.Codigo,
                    dominio.Message);

                return Problema(
                    EstadoDe(dominio),
                    dominio.Titulo,
                    dominio.Message,
                    dominio.Codigo,
                    contexto,
                    traceId);

            default:
                _log.LogError(excepcion, "Error no controlado en {Ruta}", contexto.Request.Path);

                return Problema(
                    StatusCodes.Status500InternalServerError,
                    "Error interno del servidor",
                    _entorno.IsDevelopment()
                        ? excepcion.ToString()
                        : "Ocurrio un error inesperado. Contacte al administrador con el traceId.",
                    "ERROR_INTERNO",
                    contexto,
                    traceId);
        }
    }

    private static int EstadoDe(ExcepcionDominio excepcion) => excepcion switch
    {
        RecursoNoEncontradoException => StatusCodes.Status404NotFound,
        RecursoDuplicadoException => StatusCodes.Status409Conflict,
        ConflictoConcurrenciaException => StatusCodes.Status409Conflict,
        CuentaInactivaException => StatusCodes.Status409Conflict,

        // Transitorio: el evento puede llegar en cualquier momento y el reintento
        // del cliente tiene sentido, que es justo lo que comunica un 409.
        ClienteNoSincronizadoException => StatusCodes.Status409Conflict,

        _ => StatusCodes.Status400BadRequest,
    };

    private static ProblemDetails Problema(
        int estado,
        string titulo,
        string detalle,
        string codigo,
        HttpContext contexto,
        string traceId)
    {
        var problema = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{estado}",
            Title = titulo,
            Status = estado,
            Detail = detalle,
            Instance = contexto.Request.Path,
        };

        problema.Extensions["codigo"] = codigo;
        problema.Extensions["traceId"] = traceId;

        return problema;
    }
}
