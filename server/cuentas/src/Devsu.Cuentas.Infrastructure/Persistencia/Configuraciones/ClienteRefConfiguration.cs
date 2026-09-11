using Devsu.Cuentas.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devsu.Cuentas.Infrastructure.Persistencia.Configuraciones;

public sealed class ClienteRefConfiguration : IEntityTypeConfiguration<ClienteRef>
{
    public void Configure(EntityTypeBuilder<ClienteRef> constructor)
    {
        constructor.ToTable("ClientesRef");

        constructor.HasKey(cliente => cliente.IdCliente);

        // El identificador lo asigna el microservicio de Clientes y llega dentro
        // del evento: esta base nunca lo genera.
        constructor.Property(cliente => cliente.IdCliente)
            .HasColumnName("IdCliente")
            .ValueGeneratedNever();

        constructor.Property(cliente => cliente.ClienteId)
            .HasColumnName("ClienteId").HasMaxLength(20).IsRequired();

        constructor.Property(cliente => cliente.NombreCompleto)
            .HasColumnName("NombreCompleto").HasMaxLength(256).IsRequired();

        constructor.Property(cliente => cliente.NumeroDocumento)
            .HasColumnName("NumeroDocumento").HasMaxLength(20).IsRequired();

        constructor.Property(cliente => cliente.IdEstadoCliente)
            .HasColumnName("IdEstadoCliente").IsRequired();

        constructor.Property(cliente => cliente.Activo).HasColumnName("Activo").IsRequired();

        constructor.Property(cliente => cliente.FechaSincronizacion)
            .HasColumnName("FechaSincronizacion").IsRequired();
    }
}
