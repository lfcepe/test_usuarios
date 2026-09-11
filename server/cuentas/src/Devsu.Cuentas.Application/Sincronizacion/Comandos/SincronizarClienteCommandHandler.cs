using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Domain.Catalogos;
using Devsu.Cuentas.Domain.Entidades;
using Devsu.Cuentas.Domain.Repositorios;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Devsu.Cuentas.Application.Sincronizacion.Comandos;

public sealed class SincronizarClienteCommandHandler : IRequestHandler<SincronizarClienteCommand>
{
    private readonly IRepositorioClienteRef _clientes;
    private readonly IRepositorioIdempotencia _idempotencia;
    private readonly IProveedorFechaHora _reloj;
    private readonly IUnitOfWork _unidadTrabajo;
    private readonly ILogger<SincronizarClienteCommandHandler> _log;

    public SincronizarClienteCommandHandler(
        IRepositorioClienteRef clientes,
        IRepositorioIdempotencia idempotencia,
        IProveedorFechaHora reloj,
        IUnitOfWork unidadTrabajo,
        ILogger<SincronizarClienteCommandHandler> log)
    {
        _clientes = clientes;
        _idempotencia = idempotencia;
        _reloj = reloj;
        _unidadTrabajo = unidadTrabajo;
        _log = log;
    }

    public async Task Handle(SincronizarClienteCommand peticion, CancellationToken cancelacion)
    {
        if (await _idempotencia.YaProcesadoAsync(peticion.IdMensaje, cancelacion))
        {
            _log.LogDebug("Evento {IdMensaje} descartado por duplicado", peticion.IdMensaje);
            return;
        }

        var momento = _reloj.AhoraUtc;
        var existente = await _clientes.ObtenerPorIdAsync(peticion.IdCliente, cancelacion);

        if (peticion.EsBaja)
        {
            // La referencia no se borra: los movimientos son informacion contable y
            // tienen que seguir siendo atribuibles a un titular aunque se de de baja.
            existente?.CambiarEstado(false, momento);
        }
        else if (existente is null)
        {
            await _clientes.AgregarAsync(
                ClienteRef.Crear(
                    peticion.IdCliente,
                    peticion.ClienteId ?? $"CLI-{peticion.IdCliente:D6}",
                    peticion.NombreCompleto ?? "Sin nombre",
                    peticion.NumeroDocumento ?? string.Empty,
                    peticion.IdEstadoCliente ?? CatalogoIds.EstadoCliente.Activo,
                    peticion.Activo ?? true,
                    momento),
                cancelacion);
        }
        else
        {
            existente.Actualizar(
                peticion.ClienteId ?? existente.ClienteId,
                peticion.NombreCompleto ?? existente.NombreCompleto,
                peticion.NumeroDocumento ?? existente.NumeroDocumento,
                peticion.IdEstadoCliente ?? existente.IdEstadoCliente,
                peticion.Activo ?? existente.Activo,
                momento);
        }

        await _idempotencia.RegistrarAsync(
            MensajeProcesado.Crear(peticion.IdMensaje, peticion.TipoMensaje, momento),
            cancelacion);

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
    }
}
