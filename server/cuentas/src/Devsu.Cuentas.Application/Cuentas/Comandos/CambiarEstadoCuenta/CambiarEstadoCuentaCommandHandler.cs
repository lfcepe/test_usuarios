using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Mapeos;
using Devsu.Cuentas.Domain.Excepciones;
using Devsu.Cuentas.Domain.Repositorios;
using Devsu.Contracts.Eventos;
using MediatR;

namespace Devsu.Cuentas.Application.Cuentas.Comandos.CambiarEstadoCuenta;

public sealed class CambiarEstadoCuentaCommandHandler
    : IRequestHandler<CambiarEstadoCuentaCommand, CuentaDto>
{
    private readonly IRepositorioCuenta _cuentas;
    private readonly IPublicadorEventos _eventos;
    private readonly IProveedorFechaHora _reloj;
    private readonly IUnitOfWork _unidadTrabajo;

    public CambiarEstadoCuentaCommandHandler(
        IRepositorioCuenta cuentas,
        IPublicadorEventos eventos,
        IProveedorFechaHora reloj,
        IUnitOfWork unidadTrabajo)
    {
        _cuentas = cuentas;
        _eventos = eventos;
        _reloj = reloj;
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<CuentaDto> Handle(CambiarEstadoCuentaCommand peticion, CancellationToken cancelacion)
    {
        var cuenta = await _cuentas.ObtenerParaMovimientoAsync(peticion.Id, cancelacion)
            ?? throw RecursoNoEncontradoException.Cuenta(peticion.Id);

        if (cuenta.Activa == peticion.Estado)
        {
            return cuenta.ToCuentaDto();
        }

        cuenta.CambiarEstado(peticion.Estado);

        await _eventos.EncolarAsync(
            new CuentaEstadoCambiado(
                Guid.NewGuid(),
                _reloj.AhoraUtc,
                cuenta.Id,
                cuenta.NumeroCuenta,
                cuenta.IdCliente,
                cuenta.Activa),
            cancelacion);

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        var actualizada = await _cuentas.ObtenerPorIdAsync(cuenta.Id, cancelacion);
        return (actualizada ?? cuenta).ToCuentaDto();
    }
}
