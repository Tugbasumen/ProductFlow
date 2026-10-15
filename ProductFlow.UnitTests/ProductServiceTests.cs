
using Microsoft.EntityFrameworkCore;
using ProductFlow.Application.DTOs;
using ProductFlow.Domain.Entities;
using ProductFlow.Infrastructure.Persistence;
using ProductFlow.Infrastructure.Services;

namespace ProductFlow.UnitTests;

public class ProductServiceTests
{
    private static ProductFlowDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProductFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ProductFlowDbContext(options);
    }

    private static Category CreateCategory()
    {
        return new Category("Test Kategorisi", "Test açıklaması");
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        using var dbContext = CreateDbContext();
        var service = new ProductService(dbContext);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesProduct()
    {
        using var dbContext = CreateDbContext();
        var category = CreateCategory();
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(dbContext);

        var request = new CreateProductRequest
        {
            Code = "PRD-100",
            Name = "Test Mouse",
            Description = "Test ürünü",
            Price = 250.50m,
            StockQuantity = 10,
            CategoryId = category.Id
        };

        var result = await service.CreateAsync(request);

        Assert.Equal("PRD-100", result.Code);
        Assert.Equal("Test Mouse", result.Name);
        Assert.Equal(250.50m, result.Price);
        Assert.Equal(10, result.StockQuantity);
        Assert.Equal(category.Id, result.CategoryId);
        Assert.Single(await dbContext.Products.ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_WhenProductDoesNotExist_ReturnsFalse()
    {
        using var dbContext = CreateDbContext();
        var service = new ProductService(dbContext);

        var request = new UpdateProductRequest
        {
            Name = "Güncellenmiş ürün",
            Description = "Açıklama",
            Price = 300m
        };

        var result = await service.UpdateAsync(Guid.NewGuid(), request);

        Assert.False(result);
    }

    [Fact]
    public async Task AdjustStockAsync_WhenProductDoesNotExist_ReturnsFalse()
    {
        using var dbContext = CreateDbContext();
        var service = new ProductService(dbContext);

        var result = await service.AdjustStockAsync(Guid.NewGuid(), 5);

        Assert.False(result);
    }

    [Fact]
    public async Task AdjustStockAsync_WhenStockWouldBecomeNegative_ThrowsException()
    {
        using var dbContext = CreateDbContext();
        var category = CreateCategory();
        dbContext.Categories.Add(category);

        var product = new Product(
            "PRD-101", "Test Keyboard", 500m, 3, category.Id);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AdjustStockAsync(product.Id, -5));
    }

    [Fact]
    public async Task DeactivateAsync_WhenProductDoesNotExist_ReturnsFalse()
    {
        using var dbContext = CreateDbContext();
        var service = new ProductService(dbContext);

        var result = await service.DeactivateAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task AdjustStockAsync_WithValidChange_UpdatesStock()
    {
        using var dbContext = CreateDbContext();
        var category = CreateCategory();
        dbContext.Categories.Add(category);

        var product = new Product(
            "PRD-102", "Test Mouse", 250m, 10, category.Id);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(dbContext);
        var result = await service.AdjustStockAsync(product.Id, 5);

        Assert.True(result);
        Assert.Equal(15, product.StockQuantity);
    }

    [Fact]
    public async Task DeactivateAsync_WhenProductExists_DeactivatesProduct()
    {
        using var dbContext = CreateDbContext();
        var category = CreateCategory();
        dbContext.Categories.Add(category);

        var product = new Product(
            "PRD-103", "Test Keyboard", 500m, 5, category.Id);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(dbContext);
        var result = await service.DeactivateAsync(product.Id);

        Assert.True(result);
        Assert.False(product.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductExists_UpdatesDetails()
    {
        using var dbContext = CreateDbContext();
        var category = CreateCategory();
        dbContext.Categories.Add(category);

        var product = new Product(
            "PRD-104", "Eski Ürün", 100m, 5, category.Id);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(dbContext);

        var request = new UpdateProductRequest
        {
            Name = "Yeni Ürün",
            Description = "Güncellenmiş açıklama",
            Price = 200m
        };

        var result = await service.UpdateAsync(product.Id, request);

        Assert.True(result);
        Assert.Equal("Yeni Ürün", product.Name);
        Assert.Equal("Güncellenmiş açıklama", product.Description);
        Assert.Equal(200m, product.Price);
        Assert.Equal(5, product.StockQuantity);
    }
}
