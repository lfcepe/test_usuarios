using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Mapeos;
using Devsu.Cuentas.Domain.Excepciones;
using Devsu.Cuentas.Domain.Repositorios;
using Devsu.Contracts.Eventos;
using MediatR;

namespace Devsu.Cuentas.Application.Cuentas.Comandos.ActualizarCuenta;

public sealed class ActualizarCuentaCommandHandler : IRequestHandler<ActualizarCuentaCommand, CuentaDto>
{
    private readonly IRepositorioCuenta _cuentas;
    private readonly ValidadorCatalogos _validadorCatalogos;
    private readonly IPublicadorEventos _eventos;
    private readonly IProveedorFechaHora _reloj;
    private readonly IUnitOfWork _unidadTrabajo;

    public ActualizarCuentaCommandHandler(
        IRepositorioCuenta cuentas,
        ValidadorCatalogos validadorCatalogos,
        IPublicadorEventos eventos,
        IProveedorFechaHora reloj,
        IUnitOfWork unidadTrabajo)
    {
        _cuentas = cuentas;
        _validadorCatalogos = validadorCatalogos;
        _eventos = eventos;
        _reloj = reloj;
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<CuentaDto> Handle(ActualizarCuentaCommand peticion, CancellationToken cancelacion)
    {
        var cuenta = await _cuentas.ObtenerParaMovimientoAsync(peticion.Id, cancelacion)
            ?? throw RecursoNoEncontradoException.Cuenta(peticion.Id);

        await _validadorCatalogos.ValidarTipoCuentaAsync(peticion.Datos.IdTipoCuenta, cancelacion);

        var estabaActiva = cuenta.Activa;

        cuenta.Actualizar(peticion.Datos.IdTipoCuenta, peticion.Datos.Estado);

        // El evento solo se emite si el estado cambio de verdad: publicar en cada
        // guardado inundaria al otro microservicio de mensajes sin efecto.
        if (estabaActiva != cuenta.Activa)
        {
            await _eventos.EncolarAsync(
                new CuentaEstadoCambiado(
                    Guid.NewGuid(),
                    _reloj.AhoraUtc,
                    cuenta.Id,
                    cuenta.NumeroCuenta,
                    cuenta.IdCliente,
                    cuenta.Activa),
                cancelacion);
        }

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        var actualizada = await _cuentas.ObtenerPorIdAsync(cuenta.Id, cancelacion);
        return (actualizada ?? cuenta).ToCuentaDto();
    }
}
