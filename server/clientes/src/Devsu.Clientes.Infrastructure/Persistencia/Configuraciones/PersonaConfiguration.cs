using Devsu.Clientes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devsu.Clientes.Infrastructure.Persistencia.Configuraciones;

public sealed class PersonaConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> constructor)
    {
        // Table-Per-Type: Persona y Cliente viven en tablas distintas que comparten
        // la clave primaria. Es la forma de representar la herencia que pide el
        // enunciado sin duplicar los datos personales ni dejar columnas nulas.
        constructor.UseTptMappingStrategy();

        constructor.ToTable("Personas", tabla => tabla.HasTrigger("tr_personas_edad"));

        constructor.HasKey(persona => persona.Id);
        constructor.Property(persona => persona.Id).HasColumnName("Id").ValueGeneratedOnAdd();

        constructor.Property(persona => persona.PrimerNombre)
            .HasColumnName("PrimerNombre").HasMaxLength(64).IsRequired();

        constructor.Property(persona => persona.SegundoNombre)
            .HasColumnName("SegundoNombre").HasMaxLength(64);

        constructor.Property(persona => persona.PrimerApellido)
            .HasColumnName("PrimerApellido").HasMaxLength(64).IsRequired();

        constructor.Property(persona => persona.SegundoApellido)
            .HasColumnName("SegundoApellido").HasMaxLength(64);

        constructor.Property(persona => persona.IdTipoDocumento).HasColumnName("IdTipoDocumento").IsRequired();

        constructor.Property(persona => persona.NumeroDocumento)
            .HasColumnName("NumeroDocumento").HasMaxLength(20).IsRequired();

        constructor.Property(persona => persona.IdGenero).HasColumnName("IdGenero").IsRequired();

        constructor.Property(persona => persona.DireccionDomicilio)
            .HasColumnName("DireccionDomicilio").HasMaxLength(1000).IsRequired();

        constructor.Property(persona => persona.NumeroCelular)
            .HasColumnName("NumeroCelular").HasMaxLength(10).IsRequired();

        constructor.Property(persona => persona.Email)
            .HasColumnName("Email").HasMaxLength(500).IsRequired();

        constructor.Property(persona => persona.FechaNacimiento)
            .HasColumnName("FechaNacimiento").IsRequired();

        // La escribe el trigger de PostgreSQL, nunca la aplicacion.
        constructor.Property(persona => persona.Edad)
            .HasColumnName("Edad")
            .ValueGeneratedOnAddOrUpdate();

        constructor.Property(persona => persona.FechaCreacion).HasColumnName("FechaCreacion").IsRequired();
        constructor.Property(persona => persona.FechaModificacion).HasColumnName("FechaModificacion");
        constructor.Property(persona => persona.IdEstadoPersona).HasColumnName("IdEstadoPersona").IsRequired();

        constructor.HasIndex(persona => new { persona.IdTipoDocumento, persona.NumeroDocumento })
            .IsUnique()
            .HasDatabaseName("uq_Personas_Documento");

        constructor.HasIndex(persona => persona.NumeroDocumento)
            .HasDatabaseName("ix_Personas_NumeroDocumento");

        // Restrict y no Cascade: borrar un item de catalogo no debe arrastrar personas.
        constructor.HasOne(persona => persona.TipoDocumento)
            .WithMany()
            .HasForeignKey(persona => persona.IdTipoDocumento)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(persona => persona.Genero)
            .WithMany()
            .HasForeignKey(persona => persona.IdGenero)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne(persona => persona.EstadoPersona)
            .WithMany()
            .HasForeignKey(persona => persona.IdEstadoPersona)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.Ignore(persona => persona.NombreCompleto);
        constructor.Ignore(persona => persona.Activa);
    }
}
