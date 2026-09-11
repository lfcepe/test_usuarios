using Devsu.Clientes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devsu.Clientes.Infrastructure.Persistencia.Configuraciones;

public sealed class CatalogoConfiguration : IEntityTypeConfiguration<Catalogo>
{
    public void Configure(EntityTypeBuilder<Catalogo> constructor)
    {
        constructor.ToTable("Catalogos");

        constructor.HasKey(catalogo => catalogo.Id);
        constructor.Property(catalogo => catalogo.Id).HasColumnName("Id").ValueGeneratedOnAdd();

        constructor.Property(catalogo => catalogo.DetalleCatalogo)
            .HasColumnName("DetalleCatalogo").HasMaxLength(128);

        constructor.Property(catalogo => catalogo.Item)
            .HasColumnName("Item").HasMaxLength(128);

        constructor.Property(catalogo => catalogo.IdRaiz).HasColumnName("IdRaiz");

        constructor.HasOne(catalogo => catalogo.Raiz)
            .WithMany()
            .HasForeignKey(catalogo => catalogo.IdRaiz)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
