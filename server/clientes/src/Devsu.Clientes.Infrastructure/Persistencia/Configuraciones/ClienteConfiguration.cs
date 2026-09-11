using Devsu.Clientes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devsu.Clientes.Infrastructure.Persistencia.Configuraciones;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> constructor)
    {
        constructor.ToTable("Cliente");

        constructor.Property(cliente => cliente.ClienteId)
            .HasColumnName("ClienteId").HasMaxLength(20).IsRequired();

        constructor.Property(cliente => cliente.Contrasenia)
            .HasColumnName("Contrasenia").HasMaxLength(256).IsRequired();

        constructor.Property(cliente => cliente.IdEstadoCliente)
            .HasColumnName("IdEstadoCliente").IsRequired();

        constructor.HasIndex(cliente => cliente.ClienteId)
            .IsUnique()
            .HasDatabaseName("uq_Cliente_ClienteId");

        constructor.HasOne(cliente => cliente.EstadoCliente)
            .WithMany()
            .HasForeignKey(cliente => cliente.IdEstadoCliente)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.Ignore(cliente => cliente.Activo);

        // Las columnas "FechaCreacion" y "FechaModificacion" de la tabla "Cliente"
        // no se mapean a proposito. En Table-Per-Type las propiedades declaradas en
        // la clase base pertenecen a la tabla base, asi que la auditoria del cliente
        // se lleva en "Personas". Las columnas se conservan en el script por
        // fidelidad al modelo original y las rellena su DEFAULT.
    }
}
