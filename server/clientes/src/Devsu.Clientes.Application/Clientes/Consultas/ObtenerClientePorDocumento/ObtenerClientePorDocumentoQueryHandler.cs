using Devsu.Clientes.Application.Dtos;
using Devsu.Clientes.Application.Mapeos;
using Devsu.Clientes.Domain.Catalogos;
using Devsu.Clientes.Domain.Excepciones;
using Devsu.Clientes.Domain.Repositorios;
using MediatR;

namespace Devsu.Clientes.Application.Clientes.Consultas.ObtenerClientePorDocumento;

public sealed class ObtenerClientePorDocumentoQueryHandler
    : IRequestHandler<ObtenerClientePorDocumentoQuery, ClienteDto>
{
    private readonly IRepositorioCliente _clientes;
    private readonly IRepositorioResumenCuentas _resumenes;

    public ObtenerClientePorDocumentoQueryHandler(
        IRepositorioCliente clientes,
        IRepositorioResumenCuentas resumenes)
    {
        _clientes = clientes;
        _resumenes = resumenes;
    }

    public async Task<ClienteDto> Handle(
        ObtenerClientePorDocumentoQuery peticion,
        CancellationToken cancelacion)
    {
        var tipoDocumento = peticion.IdTipoDocumento ?? CatalogoIds.TipoDocumento.Cedula;

        var cliente = await _clientes.ObtenerPorDocumentoAsync(
            tipoDocumento,
            peticion.NumeroDocumento,
            cancelacion)
            ?? throw new RecursoNoEncontradoException(
                $"No existe un cliente con el documento {peticion.NumeroDocumento}.");

        var resumen = await _resumenes.ObtenerPorClienteAsync(cliente.Id, cancelacion);

        return cliente.ToClienteDto(resumen?.TotalCuentas ?? 0);
    }
}
