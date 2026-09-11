using MediatR;

namespace Devsu.Clientes.Application.Clientes.Comandos.EliminarCliente;

/// <summary>
/// Baja de un cliente.
/// </summary>
/// <remarks>
/// Por defecto es logica: deja la fila y la marca inactiva, porque los movimientos
/// del otro microservicio son informacion contable y deben seguir siendo
/// atribuibles a alguien. El borrado fisico queda disponible con Definitivo=true
/// para cumplir literalmente el CRUD que pide el enunciado.
/// </remarks>
public sealed record EliminarClienteCommand(int Id, bool Definitivo) : IRequest;
