using Devsu.Clientes.Application.Dtos;
using Devsu.Clientes.Application.Mapeos;
using Devsu.Clientes.Domain.Excepciones;
using Devsu.Clientes.Domain.Repositorios;
using MediatR;

namespace Devsu.Clientes.Application.Clientes.Consultas.ObtenerClientePorId;

public sealed class ObtenerClientePorIdQueryHandler : IRequestHandler<ObtenerClientePorIdQuery, ClienteDto>
{
    private readonly IRepositorioCliente _clientes;
    private readonly IRepositorioResumenCuentas _resumenes;

    public ObtenerClientePorIdQueryHandler(IRepositorioCliente clientes, IRepositorioResumenCuentas resumenes)
    {
        _clientes = clientes;
        _resumenes = resumenes;
    }

    public async Task<ClienteDto> Handle(ObtenerClientePorIdQuery peticion, CancellationToken cancelacion)
    {
        var cliente = await _clientes.ObtenerPorIdAsync(peticion.Id, cancelacion)
            ?? throw RecursoNoEncontradoException.Cliente(peticion.Id);

        var resumen = await _resumenes.ObtenerPorClienteAsync(cliente.Id, cancelacion);

        return cliente.ToClienteDto(resumen?.TotalCuentas ?? 0);
    }
}
