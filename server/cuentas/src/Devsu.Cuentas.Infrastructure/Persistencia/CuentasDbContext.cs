using Devsu.Cuentas.Application.Comun;
using Devsu.Cuentas.Domain.Comun;
using Devsu.Cuentas.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Devsu.Cuentas.Infrastructure.Persistencia;

public sealed class CuentasDbContext : DbContext
{
    private readonly IProveedorFechaHora _reloj;

    public CuentasDbContext(DbContextOptions<CuentasDbContext> opciones, IProveedorFechaHora reloj)
        : base(opciones)
    {
        _reloj = reloj;
    }

    public DbSet<Cuenta> Cuentas => Set<Cuenta>();

    public DbSet<Movimiento> Movimientos => Set<Movimiento>();

    public DbSet<ClienteRef> ClientesRef => Set<ClienteRef>();

    public DbSet<Catalogo> Catalogos => Set<Catalogo>();

    public DbSet<OutboxMensaje> OutboxMensajes => Set<OutboxMensaje>();

    public DbSet<MensajeProcesado> MensajesProcesados => Set<MensajeProcesado>();

    public override Task<int> SaveChangesAsync(CancellationToken cancelacion = default)
    {
        AplicarAuditoria();
        return base.SaveChangesAsync(cancelacion);
    }

    protected override void OnModelCreating(ModelBuilder constructor)
    {
        constructor.ApplyConfigurationsFromAssembly(typeof(CuentasDbContext).Assembly);
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
}
