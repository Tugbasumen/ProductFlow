using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ProductFlow.Domain.Entities;

namespace ProductFlow.Infrastructure.Persistence;

public class ProductFlowDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ProductFlowDbContext(
        DbContextOptions<ProductFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

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

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(customer => customer.Id);

            entity.Property(customer => customer.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(customer => customer.Email)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(customer => customer.Phone)
                .HasMaxLength(20);

            entity.HasIndex(customer => customer.Email)
                .IsUnique();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);

            entity.Property(order => order.Status)
                .HasConversion<int>()
                .IsRequired();

            entity.Ignore(order => order.TotalAmount);

            entity.HasOne(order => order.Customer)
                .WithMany()
                .HasForeignKey(order => order.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(order => order.Items)
                .WithOne(item => item.Order)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(order => order.Items)
                .HasField("_items")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasIndex(order => new
            {
                order.CustomerId,
                order.CreatedAtUtc
            });
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(item => item.Id);

            entity.Property(item => item.UnitPrice)
                .HasPrecision(18, 2);

            entity.Ignore(item => item.LineTotal);

            entity.HasOne(item => item.Product)
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
