using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Mapeos;
using Devsu.Cuentas.Domain.Entidades;
using Devsu.Cuentas.Domain.Excepciones;
using Devsu.Cuentas.Domain.Repositorios;
using Devsu.Contracts.Eventos;
using MediatR;

namespace Devsu.Cuentas.Application.Cuentas.Comandos.CrearCuenta;

public sealed class CrearCuentaCommandHandler : IRequestHandler<CrearCuentaCommand, CuentaDto>
{
    private readonly IRepositorioCuenta _cuentas;
    private readonly ResolutorCliente _resolutorCliente;
    private readonly ValidadorCatalogos _validadorCatalogos;
    private readonly IPublicadorEventos _eventos;
    private readonly IProveedorFechaHora _reloj;
    private readonly IUnitOfWork _unidadTrabajo;

    public CrearCuentaCommandHandler(
        IRepositorioCuenta cuentas,
        ResolutorCliente resolutorCliente,
        ValidadorCatalogos validadorCatalogos,
        IPublicadorEventos eventos,
        IProveedorFechaHora reloj,
        IUnitOfWork unidadTrabajo)
    {
        _cuentas = cuentas;
        _resolutorCliente = resolutorCliente;
        _validadorCatalogos = validadorCatalogos;
        _eventos = eventos;
        _reloj = reloj;
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<CuentaDto> Handle(CrearCuentaCommand peticion, CancellationToken cancelacion)
    {
        var datos = peticion.Datos;

        // Verificar el titular es lo primero: si el cliente no existe o esta de baja
        // no tiene sentido seguir validando el resto.
        await _resolutorCliente.ObtenerActivoAsync(datos.IdCliente, cancelacion);

        await _validadorCatalogos.ValidarTipoCuentaAsync(datos.IdTipoCuenta, cancelacion);

        if (await _cuentas.ExisteNumeroAsync(datos.NumeroCuenta, null, cancelacion))
        {
            throw new RecursoDuplicadoException(
                $"Ya existe una cuenta con el numero {datos.NumeroCuenta}.");
        }

        return await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var cuenta = Cuenta.Crear(
                datos.IdCliente,
                datos.NumeroCuenta,
                datos.IdTipoCuenta,
                datos.SaldoInicial,
                datos.Estado);

            await _cuentas.AgregarAsync(cuenta, ct);

            // Primer guardado para obtener el Id que viaja dentro del evento.
            await _unidadTrabajo.GuardarCambiosAsync(ct);

            await _eventos.EncolarAsync(
                new CuentaAperturada(
                    Guid.NewGuid(),
                    _reloj.AhoraUtc,
                    cuenta.Id,
                    cuenta.NumeroCuenta,
                    cuenta.IdCliente,
                    cuenta.SaldoInicial),
                ct);

            await _unidadTrabajo.GuardarCambiosAsync(ct);

            var creada = await _cuentas.ObtenerPorIdAsync(cuenta.Id, ct);
            return (creada ?? cuenta).ToCuentaDto();
        }, cancelacion);
    }
}
