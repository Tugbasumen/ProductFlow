using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductFlow.Application.DTOs;
using ProductFlow.Domain.Entities;
using ProductFlow.Infrastructure.Persistence;

namespace ProductFlow.UnitTests;

public class StockMovementsApiTests : IClassFixture<ProductApiFactory>
{
    private readonly ProductApiFactory _factory;

    public StockMovementsApiTests(ProductApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<Guid> GetTestCategoryIdAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ProductFlowDbContext>();

        var category = await dbContext.Categories
            .FirstAsync(c => c.Name == "Test Kategorisi");

        return category.Id;
    }

    private async Task<Guid> CreateProductAsync(
        HttpClient client,
        int stockQuantity = 10)
    {
        var categoryId = await GetTestCategoryIdAsync();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            Code = $"MOV-{Guid.NewGuid():N}"[..12],
            Name = "Hareket Test Ürünü",
            Price = 100m,
            StockQuantity = stockQuantity,
            MinimumStockLevel = 5,
            CategoryId = categoryId
        });

        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<CreatedProduct>();
        Assert.NotNull(product);
        return product.Id;
    }

    [Fact]
    public async Task Create_IncomingMovement_ReturnsCreatedAndUpdatesStock()
    {
        await _factory.InitializeDatabaseAsync();
        using var client = _factory.CreateClient();

        var productId = await CreateProductAsync(client, 10);

        var response = await client.PostAsJsonAsync("/api/stockmovements", new
        {
            ProductId = productId,
            Type = StockMovementType.In,
            Quantity = 5,
            Description = "Depo girişi"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var movement = await response.Content
            .ReadFromJsonAsync<StockMovementResponse>();

        Assert.NotNull(movement);
        Assert.Equal(productId, movement.ProductId);
        Assert.Equal(StockMovementType.In, movement.Type);
        Assert.Equal(5, movement.Quantity);

        var productResponse = await client.GetAsync($"/api/products/{productId}");
        var product = await productResponse.Content
            .ReadFromJsonAsync<CreatedProduct>();

        Assert.Equal(15, product!.StockQuantity);
    }

    [Fact]
    public async Task Create_WhenStockWouldBecomeNegative_ReturnsBadRequest()
    {
        await _factory.InitializeDatabaseAsync();
        using var client = _factory.CreateClient();

        var productId = await CreateProductAsync(client, 2);

        var response = await client.PostAsJsonAsync("/api/stockmovements", new
        {
            ProductId = productId,
            Type = StockMovementType.Out,
            Quantity = 5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenProductDoesNotExist_ReturnsNotFound()
    {
        await _factory.InitializeDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/stockmovements", new
        {
            ProductId = Guid.NewGuid(),
            Type = StockMovementType.In,
            Quantity = 1
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsMovementsForProduct()
    {
        await _factory.InitializeDatabaseAsync();
        using var client = _factory.CreateClient();

        var productId = await CreateProductAsync(client, 10);

        await client.PostAsJsonAsync("/api/stockmovements", new
        {
            ProductId = productId,
            Type = StockMovementType.Out,
            Quantity = 2
        });

        var response = await client.GetAsync(
            $"/api/stockmovements?productId={productId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var movements = await response.Content
            .ReadFromJsonAsync<List<StockMovementResponse>>();

        Assert.NotNull(movements);
        Assert.Single(movements);
        Assert.Equal(StockMovementType.Out, movements[0].Type);
    }

    private sealed class CreatedProduct
    {
        public Guid Id { get; set; }

        public int StockQuantity { get; set; }
    }
}
