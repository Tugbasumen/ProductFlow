using Microsoft.EntityFrameworkCore;
using ProductFlow.Application.DTOs;
using ProductFlow.Domain.Entities;
using ProductFlow.Infrastructure.Persistence;
using ProductFlow.Infrastructure.Services;

namespace ProductFlow.UnitTests;

public class StockMovementServiceTests
{
    private static ProductFlowDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProductFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ProductFlowDbContext(options);
    }

    private static async Task<Product> SeedProductAsync(
        ProductFlowDbContext dbContext,
        int stockQuantity = 10)
    {
        var category = new Category("Stok Kategorisi");
        var product = new Product(
            "STK-001", "Stok Ürünü", 100m, stockQuantity, category.Id);

        dbContext.Categories.Add(category);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        return product;
    }

    [Fact]
    public async Task CreateAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        using var dbContext = CreateDbContext();
        var service = new StockMovementService(dbContext);

        var result = await service.CreateAsync(new CreateStockMovementRequest
        {
            ProductId = Guid.NewGuid(),
            Type = StockMovementType.In,
            Quantity = 5
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithIncomingMovement_IncreasesStock()
    {
        using var dbContext = CreateDbContext();
        var product = await SeedProductAsync(dbContext);
        var service = new StockMovementService(dbContext);

        var result = await service.CreateAsync(new CreateStockMovementRequest
        {
            ProductId = product.Id,
            Type = StockMovementType.In,
            Quantity = 4,
            Description = "Depo girişi"
        });

        Assert.NotNull(result);
        Assert.Equal(StockMovementType.In, result.Type);
        Assert.Equal(4, result.Quantity);
        Assert.Equal("STK-001", result.ProductCode);
        Assert.Equal(14, product.StockQuantity);
        Assert.Single(dbContext.StockMovements);
    }

    [Fact]
    public async Task CreateAsync_WithOutgoingMovement_DecreasesStock()
    {
        using var dbContext = CreateDbContext();
        var product = await SeedProductAsync(dbContext);
        var service = new StockMovementService(dbContext);

        var result = await service.CreateAsync(new CreateStockMovementRequest
        {
            ProductId = product.Id,
            Type = StockMovementType.Out,
            Quantity = 3
        });

        Assert.NotNull(result);
        Assert.Equal(7, product.StockQuantity);
    }

    [Fact]
    public async Task CreateAsync_WhenStockWouldBecomeNegative_ThrowsException()
    {
        using var dbContext = CreateDbContext();
        var product = await SeedProductAsync(dbContext, stockQuantity: 2);
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(new CreateStockMovementRequest
            {
                ProductId = product.Id,
                Type = StockMovementType.Out,
                Quantity = 5
            }));

        Assert.Equal(2, product.StockQuantity);
        Assert.Empty(dbContext.StockMovements);
    }

    [Fact]
    public async Task GetAllAsync_WithProductId_ReturnsOnlyThatProduct()
    {
        using var dbContext = CreateDbContext();
        var first = await SeedProductAsync(dbContext);
        var secondCategory = new Category("İkinci Kategori");
        var second = new Product(
            "STK-002", "İkinci Ürün", 50m, 8, secondCategory.Id);
        dbContext.Categories.Add(secondCategory);
        dbContext.Products.Add(second);
        await dbContext.SaveChangesAsync();

        var service = new StockMovementService(dbContext);

        await service.CreateAsync(new CreateStockMovementRequest
        {
            ProductId = first.Id,
            Type = StockMovementType.In,
            Quantity = 1
        });

        await service.CreateAsync(new CreateStockMovementRequest
        {
            ProductId = second.Id,
            Type = StockMovementType.Out,
            Quantity = 1
        });

        var movements = await service.GetAllAsync(first.Id);

        Assert.Single(movements);
        Assert.Equal(first.Id, movements[0].ProductId);
    }
}
