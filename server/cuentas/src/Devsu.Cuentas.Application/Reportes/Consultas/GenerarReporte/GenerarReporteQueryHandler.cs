using System.Globalization;
using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Application.Dtos;
using Devsu.Cuentas.Application.Mapeos;
using Devsu.Cuentas.Domain.Entidades;
using Devsu.Cuentas.Domain.Excepciones;
using Devsu.Cuentas.Domain.Repositorios;
using MediatR;

namespace Devsu.Cuentas.Application.Reportes.Consultas.GenerarReporte;

/// <summary>
/// Reporte de estado de cuenta por cliente y rango de fechas (F4 del enunciado).
/// </summary>
/// <param name="Cliente">Identificador numerico o codigo de negocio (CLI-000002).</param>
/// <param name="Fecha">Rango en texto, tal como lo plantea el enunciado.</param>
public sealed record GenerarReporteQuery(
    string? Cliente,
    string? Fecha,
    DateOnly? FechaInicio,
    DateOnly? FechaFin) : IRequest<ReporteEstadoCuentaDto>;

public sealed class GenerarReporteQueryHandler
    : IRequestHandler<GenerarReporteQuery, ReporteEstadoCuentaDto>
{
    private readonly IRepositorioCuenta _cuentas;
    private readonly IRepositorioMovimiento _movimientos;
    private readonly IRepositorioClienteRef _clientes;
    private readonly IProveedorFechaHora _reloj;

    public GenerarReporteQueryHandler(
        IRepositorioCuenta cuentas,
        IRepositorioMovimiento movimientos,
        IRepositorioClienteRef clientes,
        IProveedorFechaHora reloj)
    {
        _cuentas = cuentas;
        _movimientos = movimientos;
        _clientes = clientes;
        _reloj = reloj;
    }

    public async Task<ReporteEstadoCuentaDto> Handle(
        GenerarReporteQuery peticion,
        CancellationToken cancelacion)
    {
        var rango = RangoFechas.Resolver(
            peticion.Fecha,
            peticion.FechaInicio,
            peticion.FechaFin,
            _reloj.HoyUtc);

        var cliente = await LocalizarClienteAsync(peticion.Cliente, cancelacion);

        var cuentas = await _cuentas.ObtenerPorClienteAsync(cliente.IdCliente, cancelacion);

        if (cuentas.Count == 0)
        {
            return new ReporteEstadoCuentaDto(
                cliente.IdCliente,
                cliente.ClienteId,
                cliente.NombreCompleto,
                rango.Inicio,
                rango.Fin,
                0,
                0,
                Array.Empty<ReporteCuentaDto>());
        }

        // Una sola consulta para los movimientos de todas las cuentas del cliente:
        // con una consulta por cuenta, un cliente con diez cuentas costaria once
        // viajes a la base para el mismo resultado.
        var movimientos = await _movimientos.ObtenerParaReporteAsync(
            cuentas.Select(cuenta => cuenta.Id).ToArray(),
            rango.InicioUtc,
            rango.FinUtc,
            cancelacion);

        var porCuenta = movimientos
            .GroupBy(movimiento => movimiento.IdCuentaPersona)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList());

        var detalle = cuentas
            .Select(cuenta => ConstruirDetalle(cuenta, porCuenta.GetValueOrDefault(cuenta.Id)))
            .ToList();

        return new ReporteEstadoCuentaDto(
            cliente.IdCliente,
            cliente.ClienteId,
            cliente.NombreCompleto,
            rango.Inicio,
            rango.Fin,
            detalle.Count,
            detalle.Sum(cuenta => cuenta.Movimientos.Count),
            detalle);
    }

    /// <summary>
    /// Aplana el reporte al formato literal del ejemplo del enunciado.
    /// </summary>
    /// <remarks>
    /// Solo se emiten filas de cuentas con movimientos en el rango, que es lo que
    /// muestra el ejemplo. El detalle completo, incluidas las cuentas sin actividad,
    /// esta en la version agrupada del reporte.
    /// </remarks>
    public static IReadOnlyList<ReporteFilaDto> Aplanar(ReporteEstadoCuentaDto reporte) =>
        reporte.Cuentas
            .SelectMany(cuenta => cuenta.Movimientos.Select(movimiento => new ReporteFilaDto
            {
                Fecha = movimiento.Fecha.ToString("d/M/yyyy", CultureInfo.InvariantCulture),
                Cliente = reporte.Cliente,
                NumeroCuenta = cuenta.NumeroCuenta,
                Tipo = cuenta.TipoCuenta ?? string.Empty,
                SaldoInicial = cuenta.SaldoInicial,
                Estado = cuenta.Estado,
                Movimiento = movimiento.Valor,
                SaldoDisponible = movimiento.Saldo,
            }))
            .ToList();

    private static ReporteCuentaDto ConstruirDetalle(Cuenta cuenta, List<Movimiento>? movimientos)
    {
        var lista = movimientos ?? new List<Movimiento>();

        var ordenados = lista
            .OrderBy(movimiento => movimiento.Fecha)
            .ThenBy(movimiento => movimiento.Id)
            .Select(movimiento => movimiento.ToMovimientoDto(cuenta))
            .ToList();

        return new ReporteCuentaDto(
            cuenta.Id,
            cuenta.NumeroCuenta,
            cuenta.TipoCuenta?.Item,
            cuenta.SaldoInicial,
            cuenta.SaldoDisponible,
            cuenta.Activa,
            lista.Where(movimiento => movimiento.Valor < 0).Sum(movimiento => movimiento.Valor),
            lista.Where(movimiento => movimiento.Valor > 0).Sum(movimiento => movimiento.Valor),
            ordenados);
    }

    private async Task<ClienteRef> LocalizarClienteAsync(string? cliente, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(cliente))
        {
            throw new ReglaNegocioException(
                "Debe indicar el cliente del reporte mediante su identificador o su codigo CLI-000000.");
        }

        var texto = cliente.Trim();

        var encontrado = int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? await _clientes.ObtenerPorIdAsync(id, cancelacion)
            : await _clientes.ObtenerPorClienteIdAsync(texto, cancelacion);

        return encontrado
            ?? throw new RecursoNoEncontradoException($"No existe el cliente '{texto}' en este microservicio.");
    }
}
