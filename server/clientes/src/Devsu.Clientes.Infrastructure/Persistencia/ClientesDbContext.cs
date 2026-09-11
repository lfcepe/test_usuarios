using Devsu.Clientes.Application.Comun;
using Devsu.Clientes.Domain.Comun;
using Devsu.Clientes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Clientes.Infrastructure.Persistencia;

public sealed class ClientesDbContext : DbContext
{
    private readonly IProveedorFechaHora _reloj;

    public ClientesDbContext(DbContextOptions<ClientesDbContext> opciones, IProveedorFechaHora reloj)
        : base(opciones)
    {
        _reloj = reloj;
    }

    public DbSet<Persona> Personas => Set<Persona>();

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Catalogo> Catalogos => Set<Catalogo>();

    public DbSet<ResumenCuentasCliente> ResumenCuentasClientes => Set<ResumenCuentasCliente>();

    public DbSet<OutboxMensaje> OutboxMensajes => Set<OutboxMensaje>();

    public DbSet<MensajeProcesado> MensajesProcesados => Set<MensajeProcesado>();

    public override Task<int> SaveChangesAsync(CancellationToken cancelacion = default)
    {
        AplicarAuditoria();
        AplicarEdadSinTrigger();
        return base.SaveChangesAsync(cancelacion);
    }

    protected override void OnModelCreating(ModelBuilder constructor)
    {
        constructor.ApplyConfigurationsFromAssembly(typeof(ClientesDbContext).Assembly);

        // En PostgreSQL la edad la escribe el trigger tr_personas_edad y EF solo la
        // lee. Con cualquier otro proveedor no existe ese trigger (las pruebas de
        // integracion usan SQLite), asi que la columna pasa a ser escribible y el
        // valor lo calcula AplicarEdadSinTrigger. La regla de negocio es la misma
        // en los dos casos: Persona.CalcularEdad replica fn_calcular_edad.
        if (!Database.IsNpgsql())
        {
            constructor.Entity<Persona>().Property(persona => persona.Edad).ValueGeneratedNever();
        }
    }

    private void AplicarAuditoria()
    {
        var momento = _reloj.AhoraUtc;

        foreach (var entrada in ChangeTracker.Entries<EntidadAuditable>())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    entrada.Entity.RegistrarCreacion(momento);
                    break;

                case EntityState.Modified:
                    entrada.Entity.RegistrarModificacion(momento);
                    break;
            }
        }
    }

    private void AplicarEdadSinTrigger()
    {
        if (Database.IsNpgsql())
        {
            return;
        }

        var hoy = _reloj.HoyUtc;

        foreach (var entrada in ChangeTracker.Entries<Persona>())
        {
            if (entrada.State is EntityState.Added or EntityState.Modified)
            {
                entrada.Entity.EstablecerEdad(Persona.CalcularEdad(entrada.Entity.FechaNacimiento, hoy));
            }
        }
    }
}
