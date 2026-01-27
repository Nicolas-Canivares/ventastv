using Microsoft.EntityFrameworkCore;
using PhantomTvSales.Api.Models;

namespace PhantomTvSales.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Sale> Sales => Set<Sale>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>()
            .HasIndex(c => c.PhantomId)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
