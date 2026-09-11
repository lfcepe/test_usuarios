using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Mapeos;
using Devsu.Cuentas.Domain.Excepciones;
using Devsu.Cuentas.Domain.Repositorios;
using MediatR;

namespace Devsu.Cuentas.Application.Cuentas.Consultas.ObtenerCuenta;

public sealed class ObtenerCuentaPorIdQueryHandler : IRequestHandler<ObtenerCuentaPorIdQuery, CuentaDto>
{
    private readonly IRepositorioCuenta _cuentas;

    public ObtenerCuentaPorIdQueryHandler(IRepositorioCuenta cuentas)
    {
        _cuentas = cuentas;
    }

    public async Task<CuentaDto> Handle(ObtenerCuentaPorIdQuery peticion, CancellationToken cancelacion)
    {
        var cuenta = await _cuentas.ObtenerPorIdAsync(peticion.Id, cancelacion)
            ?? throw RecursoNoEncontradoException.Cuenta(peticion.Id);

        return cuenta.ToCuentaDto();
    }
}

public sealed class ObtenerCuentaPorNumeroQueryHandler
    : IRequestHandler<ObtenerCuentaPorNumeroQuery, CuentaDto>
{
    private readonly IRepositorioCuenta _cuentas;

    public ObtenerCuentaPorNumeroQueryHandler(IRepositorioCuenta cuentas)
    {
        _cuentas = cuentas;
    }

    public async Task<CuentaDto> Handle(
        ObtenerCuentaPorNumeroQuery peticion,
        CancellationToken cancelacion)
    {
        var cuenta = await _cuentas.ObtenerPorNumeroAsync(peticion.NumeroCuenta, cancelacion)
            ?? throw RecursoNoEncontradoException.Cuenta(peticion.NumeroCuenta);

        return cuenta.ToCuentaDto();
    }
}
