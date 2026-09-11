using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Domain.Entidades;
using Devsu.Clientes.Domain.Repositorios;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Devsu.Clientes.Application.Sincronizacion.Comandos;

public sealed class SincronizarCuentaCommandHandler : IRequestHandler<SincronizarCuentaCommand>
{
    private readonly IRepositorioResumenCuentas _resumenes;
    private readonly IRepositorioCliente _clientes;
    private readonly IRepositorioIdempotencia _idempotencia;
    private readonly IProveedorFechaHora _reloj;
    private readonly IUnitOfWork _unidadTrabajo;
    private readonly ILogger<SincronizarCuentaCommandHandler> _log;

    public SincronizarCuentaCommandHandler(
        IRepositorioResumenCuentas resumenes,
        IRepositorioCliente clientes,
        IRepositorioIdempotencia idempotencia,
        IProveedorFechaHora reloj,
        IUnitOfWork unidadTrabajo,
        ILogger<SincronizarCuentaCommandHandler> log)
    {
        _resumenes = resumenes;
        _clientes = clientes;
        _idempotencia = idempotencia;
        _reloj = reloj;
        _unidadTrabajo = unidadTrabajo;
        _log = log;
    }

    public async Task Handle(SincronizarCuentaCommand peticion, CancellationToken cancelacion)
    {
        if (await _idempotencia.YaProcesadoAsync(peticion.IdMensaje, cancelacion))
        {
            _log.LogDebug("Evento {IdMensaje} descartado por duplicado", peticion.IdMensaje);
            return;
        }

        var resumen = await _resumenes.ObtenerPorClienteAsync(peticion.IdCliente, cancelacion);

        if (resumen is null)
        {
            // Puede pasar si el cliente se creo antes de que existiera el read model.
            // Solo se crea la fila si el cliente existe, porque hay una FK contra "Cliente".
            var cliente = await _clientes.ObtenerPorIdAsync(peticion.IdCliente, cancelacion);
            if (cliente is null)
            {
                _log.LogWarning(
                    "Evento de cuenta para el cliente {IdCliente}, que no existe en esta base",
                    peticion.IdCliente);
                return;
            }

            resumen = ResumenCuentasCliente.Crear(peticion.IdCliente, _reloj.AhoraUtc);
            await _resumenes.AgregarAsync(resumen, cancelacion);
        }

        switch (peticion.Cambio)
        {
            case TipoCambioCuenta.Apertura:
            case TipoCambioCuenta.Activacion:
                resumen.Incrementar(_reloj.AhoraUtc);
                break;

            case TipoCambioCuenta.Desactivacion:
                resumen.Decrementar(_reloj.AhoraUtc);
                break;
        }

        await _idempotencia.RegistrarAsync(
            MensajeProcesado.Crear(peticion.IdMensaje, peticion.TipoMensaje, _reloj.AhoraUtc),
            cancelacion);

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
    }
}
