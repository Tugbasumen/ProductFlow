using Microsoft.EntityFrameworkCore;
using ProductFlow.Domain.Entities;

namespace ProductFlow.Infrastructure.Persistence;

public class ProductFlowDbContext : DbContext
{
    public ProductFlowDbContext(
    DbContextOptions<ProductFlowDbContext> options)
    : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Price)
                .HasPrecision(18, 2);
        });
    }


}
