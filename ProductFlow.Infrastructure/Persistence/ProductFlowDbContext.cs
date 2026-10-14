
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

    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Price)
                .HasPrecision(18, 2);

            entity.Property(p => p.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(p => p.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(p => p.Code)
                .IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);

            entity.Property(category => category.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(category => category.Description)
                .HasMaxLength(500);

            entity.HasIndex(category => category.Name)
                .IsUnique();
        });
    }
}
