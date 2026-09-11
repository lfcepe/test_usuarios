using System.Diagnostics;
using Devsu.Clientes.Domain.Excepciones;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Devsu.Clientes.Api.Middleware;

/// <summary>
/// Traduce cualquier excepcion no controlada a una respuesta ProblemDetails.
/// </summary>
/// <remarks>
/// Centralizarlo aqui evita bloques try/catch repetidos en los controladores y
/// garantiza que el formato del error sea siempre el mismo, incluidos el codigo
/// de negocio y el identificador de traza para poder cruzarlo con los logs.
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
            // Ya se envio parte del cuerpo; reescribir la cabecera aqui produciria
            // una respuesta corrupta. Solo queda registrar el fallo.
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

            case ExcepcionDominio dominio:
                _log.LogInformation(
                    "Regla de dominio incumplida ({Codigo}): {Mensaje}",
                    dominio.Codigo,
                    dominio.Message);

                return Problema(
                    EstadoDe(dominio),
                    TituloDe(dominio),
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
        _ => StatusCodes.Status400BadRequest,
    };

    private static string TituloDe(ExcepcionDominio excepcion) => excepcion switch
    {
        RecursoNoEncontradoException => "Recurso no encontrado",
        RecursoDuplicadoException => "El recurso ya existe",
        ConflictoConcurrenciaException => "Conflicto de concurrencia",
        _ => "No se pudo completar la operacion",
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
