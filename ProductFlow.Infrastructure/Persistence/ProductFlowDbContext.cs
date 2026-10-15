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
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

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

            entity.Property(p => p.MinimumStockLevel)
                .HasDefaultValue(5);

            entity.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
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
        modelBuilder.Entity<StockMovement>(entity =>
{
    entity.HasKey(movement => movement.Id);

    entity.Property(movement => movement.Type)
        .HasConversion<int>()
        .IsRequired();

    entity.Property(movement => movement.Quantity)
        .IsRequired();

    entity.Property(movement => movement.Description)
        .HasMaxLength(500);

    entity.Property(movement => movement.CreatedAtUtc)
        .IsRequired();

    entity.HasOne(movement => movement.Product)
        .WithMany()
        .HasForeignKey(movement => movement.ProductId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasIndex(movement => new
    {
        movement.ProductId,
        movement.CreatedAtUtc
    });
});
    }
}
