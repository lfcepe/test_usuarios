using Devsu.Cuentas.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devsu.Cuentas.Infrastructure.Persistencia.Configuraciones;

public sealed class CuentaConfiguration : IEntityTypeConfiguration<Cuenta>
{
    public void Configure(EntityTypeBuilder<Cuenta> constructor)
    {
        constructor.ToTable("CuentasPersona");

        constructor.HasKey(cuenta => cuenta.Id);
        constructor.Property(cuenta => cuenta.Id).HasColumnName("Id").ValueGeneratedOnAdd();

        constructor.Property(cuenta => cuenta.IdCliente).HasColumnName("IdCliente").IsRequired();

        constructor.Property(cuenta => cuenta.NumeroCuenta)
            .HasColumnName("NumeroCuenta").HasMaxLength(20).IsRequired();

        constructor.Property(cuenta => cuenta.IdTipoCuenta).HasColumnName("IdTipoCuenta").IsRequired();

        // La precision se declara explicitamente: con el valor por defecto de
        // Npgsql un decimal sin escala puede redondear importes de forma silenciosa.
        constructor.Property(cuenta => cuenta.SaldoInicial)
            .HasColumnName("SaldoInicial").HasPrecision(18, 2).IsRequired();

        constructor.Property(cuenta => cuenta.SaldoDisponible)
            .HasColumnName("SaldoDisponible").HasPrecision(18, 2).IsRequired();

        constructor.Property(cuenta => cuenta.FechaCreacion).HasColumnName("FechaCreacion").IsRequired();
        constructor.Property(cuenta => cuenta.FechaModificacion).HasColumnName("FechaModificacion");
        constructor.Property(cuenta => cuenta.IdEstadoCuenta).HasColumnName("IdEstadoCuenta").IsRequired();

        constructor.HasIndex(cuenta => cuenta.NumeroCuenta)
            .IsUnique()
            .HasDatabaseName("uq_CuentasPersona_NumeroCuenta");

        constructor.HasIndex(cuenta => cuenta.IdCliente)
            .HasDatabaseName("ix_CuentasPersona_IdCliente");

        constructor.HasOne(cuenta => cuenta.Cliente)
            .WithMany()
            .HasForeignKey(cuenta => cuenta.IdCliente)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(cuenta => cuenta.TipoCuenta)
            .WithMany()
            .HasForeignKey(cuenta => cuenta.IdTipoCuenta)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(cuenta => cuenta.EstadoCuenta)
            .WithMany()
            .HasForeignKey(cuenta => cuenta.IdEstadoCuenta)
            .OnDelete(DeleteBehavior.Restrict);

        // La coleccion de movimientos se expone como IReadOnlyList y se respalda en
        // un campo privado: EF tiene que escribir directamente en el campo para no
        // saltarse las reglas del agregado.
        constructor.Metadata
            .FindNavigation(nameof(Cuenta.Movimientos))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        constructor.HasMany(cuenta => cuenta.Movimientos)
            .WithOne(movimiento => movimiento.Cuenta!)
            .HasForeignKey(movimiento => movimiento.IdCuentaPersona)
            .OnDelete(DeleteBehavior.Cascade);

        constructor.Ignore(cuenta => cuenta.Activa);
    }
}
