using Devsu.Clientes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devsu.Clientes.Infrastructure.Persistencia.Configuraciones;

public sealed class ResumenCuentasClienteConfiguration : IEntityTypeConfiguration<ResumenCuentasCliente>
{
    public void Configure(EntityTypeBuilder<ResumenCuentasCliente> constructor)
    {
        constructor.ToTable("ResumenCuentasCliente");

        constructor.HasKey(resumen => resumen.IdCliente);

        constructor.Property(resumen => resumen.IdCliente)
            .HasColumnName("IdCliente")
            .ValueGeneratedNever();

        constructor.Property(resumen => resumen.TotalCuentas).HasColumnName("TotalCuentas").IsRequired();

        constructor.Property(resumen => resumen.FechaActualizacion)
            .HasColumnName("FechaActualizacion").IsRequired();

        constructor.HasOne<Cliente>()
            .WithOne()
            .HasForeignKey<ResumenCuentasCliente>(resumen => resumen.IdCliente)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
