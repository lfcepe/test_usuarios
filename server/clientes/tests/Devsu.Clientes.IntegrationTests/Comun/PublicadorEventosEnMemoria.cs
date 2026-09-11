using Devsu.Clientes.Application.Comun;
using Devsu.Contracts.Eventos;

namespace Devsu.Clientes.IntegrationTests.Comun;

/// <summary>
/// Sustituye al publicador real durante las pruebas y guarda lo publicado para
/// poder afirmar sobre ello.
/// </summary>
/// <remarks>
/// Se registra como singleton para que la lista sobreviva a los scopes de cada
/// peticion HTTP; con un scoped, cada peticion tendria su propia lista vacia.
/// </remarks>
public sealed class PublicadorEventosEnMemoria : IPublicadorEventos
{
    private readonly List<IEventoIntegracion> _eventos = new();
    private readonly object _candado = new();

    public IReadOnlyList<IEventoIntegracion> Eventos
    {
        get
        {
            lock (_candado)
            {
                return _eventos.ToList();
            }
        }
    }

    public Task EncolarAsync(IEventoIntegracion evento, CancellationToken cancelacion)
    {
        lock (_candado)
        {
            _eventos.Add(evento);
        }

        return Task.CompletedTask;
    }

    public void Limpiar()
    {
        lock (_candado)
        {
            _eventos.Clear();
        }
    }
}
