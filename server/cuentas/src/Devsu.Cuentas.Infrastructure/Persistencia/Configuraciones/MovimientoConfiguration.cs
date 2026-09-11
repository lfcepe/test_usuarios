using Devsu.Cuentas.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devsu.Cuentas.Infrastructure.Persistencia.Configuraciones;

public sealed class MovimientoConfiguration : IEntityTypeConfiguration<Movimiento>
{
    public void Configure(EntityTypeBuilder<Movimiento> constructor)
    {
        constructor.ToTable("Movimientos");

        constructor.HasKey(movimiento => movimiento.Id);
        constructor.Property(movimiento => movimiento.Id).HasColumnName("Id").ValueGeneratedOnAdd();

        constructor.Property(movimiento => movimiento.IdCuentaPersona)
            .HasColumnName("IdCuentaPersona").IsRequired();

        constructor.Property(movimiento => movimiento.Fecha).HasColumnName("Fecha").IsRequired();

        constructor.Property(movimiento => movimiento.IdTipoMovimiento)
            .HasColumnName("IdTipoMovimiento").IsRequired();

        constructor.Property(movimiento => movimiento.Valor)
            .HasColumnName("Valor").HasPrecision(18, 2).IsRequired();

        constructor.Property(movimiento => movimiento.Saldo)
            .HasColumnName("Saldo").HasPrecision(18, 2).IsRequired();

        constructor.Property(movimiento => movimiento.Descripcion)
            .HasColumnName("Descripcion").HasMaxLength(256);

        constructor.Property(movimiento => movimiento.FechaCreacion)
            .HasColumnName("FechaCreacion").IsRequired();

        constructor.Property(movimiento => movimiento.FechaModificacion)
            .HasColumnName("FechaModificacion");

        constructor.Property(movimiento => movimiento.IdEstadoMovimiento)
            .HasColumnName("IdEstadoMovimiento").IsRequired();

        constructor.HasIndex(movimiento => new { movimiento.IdCuentaPersona, movimiento.Fecha })
            .HasDatabaseName("ix_Movimientos_Cuenta_Fecha");

        constructor.HasOne(movimiento => movimiento.TipoMovimiento)
            .WithMany()
            .HasForeignKey(movimiento => movimiento.IdTipoMovimiento)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(movimiento => movimiento.EstadoMovimiento)
            .WithMany()
            .HasForeignKey(movimiento => movimiento.IdEstadoMovimiento)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.Ignore(movimiento => movimiento.EsRetiro);
        constructor.Ignore(movimiento => movimiento.EsDeposito);
    }
}
