using Devsu.Cuentas.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devsu.Cuentas.Infrastructure.Persistencia.Configuraciones;

public sealed class MensajeProcesadoConfiguration : IEntityTypeConfiguration<MensajeProcesado>
{
    public void Configure(EntityTypeBuilder<MensajeProcesado> constructor)
    {
        constructor.ToTable("MensajesProcesados");

        constructor.HasKey(mensaje => mensaje.IdMensaje);
        constructor.Property(mensaje => mensaje.IdMensaje).HasColumnName("IdMensaje").ValueGeneratedNever();

        constructor.Property(mensaje => mensaje.TipoMensaje)
            .HasColumnName("TipoMensaje").HasMaxLength(256).IsRequired();

        constructor.Property(mensaje => mensaje.FechaProcesado).HasColumnName("FechaProcesado").IsRequired();
    }
}
