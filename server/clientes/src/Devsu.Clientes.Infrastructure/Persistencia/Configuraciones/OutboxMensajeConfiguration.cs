using Devsu.Clientes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devsu.Clientes.Infrastructure.Persistencia.Configuraciones;

public sealed class OutboxMensajeConfiguration : IEntityTypeConfiguration<OutboxMensaje>
{
    public void Configure(EntityTypeBuilder<OutboxMensaje> constructor)
    {
        constructor.ToTable("OutboxMensajes");

        constructor.HasKey(mensaje => mensaje.Id);
        constructor.Property(mensaje => mensaje.Id).HasColumnName("Id").ValueGeneratedNever();

        constructor.Property(mensaje => mensaje.TipoMensaje)
            .HasColumnName("TipoMensaje").HasMaxLength(256).IsRequired();

        constructor.Property(mensaje => mensaje.Contenido).HasColumnName("Contenido").IsRequired();
        constructor.Property(mensaje => mensaje.FechaCreacion).HasColumnName("FechaCreacion").IsRequired();
        constructor.Property(mensaje => mensaje.FechaProcesado).HasColumnName("FechaProcesado");
        constructor.Property(mensaje => mensaje.Intentos).HasColumnName("Intentos").IsRequired();
        constructor.Property(mensaje => mensaje.Error).HasColumnName("Error");

        constructor.HasIndex(mensaje => mensaje.FechaCreacion)
            .HasDatabaseName("ix_OutboxMensajes_Fecha");
    }
}
