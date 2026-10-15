using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductFlow.Application.DTOs;
using ProductFlow.Domain.Entities;
using ProductFlow.Infrastructure.Persistence;

namespace ProductFlow.UnitTests;

public class OrdersApiTests : IClassFixture<ProductApiFactory>
{
    private readonly ProductApiFactory _factory;

    public OrdersApiTests(ProductApiFactory factory)
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

    private async Task<(Guid CustomerId, Guid ProductId)> SeedAsync(
        HttpClient client,
        int stockQuantity = 10)
    {
        var categoryId = await GetTestCategoryIdAsync();

        var productResponse = await client.PostAsJsonAsync("/api/products", new
        {
            Code = $"ORD-{Guid.NewGuid():N}"[..12],
            Name = "Sipariş Test Ürünü",
            Price = 100m,
            StockQuantity = stockQuantity,
            MinimumStockLevel = 5,
            CategoryId = categoryId
        });

        productResponse.EnsureSuccessStatusCode();
        var product = await productResponse.Content
            .ReadFromJsonAsync<CreatedEntity>();

        var customerResponse = await client.PostAsJsonAsync("/api/customers", new
        {
            Name = "Sipariş Müşterisi",
            Email = $"order-{Guid.NewGuid():N}@example.com"
        });

        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content
            .ReadFromJsonAsync<CreatedEntity>();

        return (customer!.Id, product!.Id);
    }

    [Fact]
    public async Task CreateOrder_DecreasesStock()
    {
        await _factory.InitializeDatabaseAsync();
        using var client = _factory.CreateClient();

        var (customerId, productId) = await SeedAsync(client, 10);

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            CustomerId = customerId,
            Items = new[]
            {
                new { ProductId = productId, Quantity = 2 }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(200m, order.TotalAmount);

        var productResponse = await client.GetAsync($"/api/products/{productId}");
        var product = await productResponse.Content
            .ReadFromJsonAsync<ProductStockResponse>();

        Assert.Equal(8, product!.StockQuantity);
    }

    [Fact]
    public async Task CreateOrder_WhenStockIsInsufficient_ReturnsBadRequest()
    {
        await _factory.InitializeDatabaseAsync();
        using var client = _factory.CreateClient();

        var (customerId, productId) = await SeedAsync(client, 1);

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            CustomerId = customerId,
            Items = new[]
            {
                new { ProductId = productId, Quantity = 5 }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmOrder_WhenPending_ReturnsConfirmed()
    {
        await _factory.InitializeDatabaseAsync();
        using var client = _factory.CreateClient();

        var (customerId, productId) = await SeedAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/orders", new
        {
            CustomerId = customerId,
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1 }
            }
        });

        var created = await createResponse.Content
            .ReadFromJsonAsync<OrderResponse>();

        var confirmResponse = await client.PatchAsync(
            $"/api/orders/{created!.Id}/confirm",
            null);

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var confirmed = await confirmResponse.Content
            .ReadFromJsonAsync<OrderResponse>();

        Assert.Equal(OrderStatus.Confirmed, confirmed!.Status);
    }

    [Fact]
    public async Task CancelOrder_RestoresStock()
    {
        await _factory.InitializeDatabaseAsync();
        using var client = _factory.CreateClient();

        var (customerId, productId) = await SeedAsync(client, 10);

        var createResponse = await client.PostAsJsonAsync("/api/orders", new
        {
            CustomerId = customerId,
            Items = new[]
            {
                new { ProductId = productId, Quantity = 3 }
            }
        });

        var created = await createResponse.Content
            .ReadFromJsonAsync<OrderResponse>();

        var cancelResponse = await client.PatchAsync(
            $"/api/orders/{created!.Id}/cancel",
            null);

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        var productResponse = await client.GetAsync($"/api/products/{productId}");
        var product = await productResponse.Content
            .ReadFromJsonAsync<ProductStockResponse>();

        Assert.Equal(10, product!.StockQuantity);
    }

    private sealed class CreatedEntity
    {
        public Guid Id { get; set; }
    }

    private sealed class ProductStockResponse
    {
        public int StockQuantity { get; set; }
    }
}
