using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Mapeos;
using Devsu.Cuentas.Domain.Entidades;
using Devsu.Cuentas.Domain.Excepciones;
using Devsu.Cuentas.Domain.Repositorios;
using MediatR;

namespace Devsu.Cuentas.Application.Movimientos.Comandos.RegistrarMovimiento;

/// <summary>
/// Orquesta el registro de un movimiento.
/// </summary>
/// <remarks>
/// El manejador no calcula saldos ni decide si hay fondos: eso lo hace
/// Cuenta.RegistrarMovimiento. Aqui solo se localiza el agregado, se comprueban
/// las precondiciones que dependen de la base y se confirma la transaccion.
/// </remarks>
public sealed class RegistrarMovimientoCommandHandler
    : IRequestHandler<RegistrarMovimientoCommand, MovimientoDto>
{
    private readonly IRepositorioCuenta _cuentas;
    private readonly IRepositorioMovimiento _movimientos;
    private readonly ResolutorCliente _resolutorCliente;
    private readonly ValidadorCatalogos _validadorCatalogos;
    private readonly IProveedorFechaHora _reloj;
    private readonly IUnitOfWork _unidadTrabajo;

    public RegistrarMovimientoCommandHandler(
        IRepositorioCuenta cuentas,
        IRepositorioMovimiento movimientos,
        ResolutorCliente resolutorCliente,
        ValidadorCatalogos validadorCatalogos,
        IProveedorFechaHora reloj,
        IUnitOfWork unidadTrabajo)
    {
        _cuentas = cuentas;
        _movimientos = movimientos;
        _resolutorCliente = resolutorCliente;
        _validadorCatalogos = validadorCatalogos;
        _reloj = reloj;
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<MovimientoDto> Handle(
        RegistrarMovimientoCommand peticion,
        CancellationToken cancelacion)
    {
        var datos = peticion.Datos;

        await _validadorCatalogos.ValidarTipoMovimientoAsync(datos.IdTipoMovimiento, cancelacion);

        // La cuenta se carga con bloqueo pesimista dentro de la transaccion: dos
        // retiros simultaneos sobre la misma cuenta deben serializarse o los dos
        // leerian el mismo saldo y la dejarian en descubierto.
        return await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var cuenta = await LocalizarCuentaAsync(datos.IdCuenta, datos.NumeroCuenta, ct);

            await _resolutorCliente.ObtenerActivoAsync(cuenta.IdCliente, ct);

            var fecha = datos.Fecha ?? _reloj.AhoraUtc;

            var movimiento = cuenta.RegistrarMovimiento(
                datos.Valor,
                fecha,
                datos.IdTipoMovimiento,
                datos.Descripcion);

            await _unidadTrabajo.GuardarCambiosAsync(ct);

            // Se relee para devolver las descripciones de catalogo. La entidad recien
            // creada solo tiene los identificadores: sus navegaciones no estan cargadas
            // y el cliente recibiria tipoMovimiento en null.
            var persistido = await _movimientos.ObtenerPorIdAsync(movimiento.Id, ct);

            return persistido?.ToMovimientoDto() ?? movimiento.ToMovimientoDto(cuenta);
        }, cancelacion);
    }

    private async Task<Cuenta> LocalizarCuentaAsync(
        int? idCuenta,
        string? numeroCuenta,
        CancellationToken cancelacion)
    {
        if (idCuenta is > 0)
        {
            return await _cuentas.ObtenerParaMovimientoAsync(idCuenta.Value, cancelacion)
                ?? throw RecursoNoEncontradoException.Cuenta(idCuenta.Value);
        }

        if (!string.IsNullOrWhiteSpace(numeroCuenta))
        {
            return await _cuentas.ObtenerParaMovimientoPorNumeroAsync(numeroCuenta.Trim(), cancelacion)
                ?? throw RecursoNoEncontradoException.Cuenta(numeroCuenta.Trim());
        }

        throw new ReglaNegocioException("Debe indicar idCuenta o numeroCuenta.");
    }
}
