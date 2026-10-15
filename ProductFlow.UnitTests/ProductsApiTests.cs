
using System;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductFlow.Infrastructure.Persistence;

namespace ProductFlow.UnitTests;

public class ProductsApiTests : IClassFixture<ProductApiFactory>
{
    private readonly ProductApiFactory _factory;

    public ProductsApiTests(ProductApiFactory factory)
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

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithValidRequest_ReturnsCreated()
    {
        await _factory.InitializeDatabaseAsync();
        var categoryId = await GetTestCategoryIdAsync();

        using var client = _factory.CreateClient();

        var request = new
        {
            Code = "TEST-001",
            Name = "Test Mouse",
            Description = "Entegrasyon testi ürünü",
            Price = 250.50m,
            StockQuantity = 10,
            CategoryId = categoryId
        };

        var response = await client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task GetById_WhenProductExists_ReturnsProduct()
    {
        await _factory.InitializeDatabaseAsync();
        var categoryId = await GetTestCategoryIdAsync();

        using var client = _factory.CreateClient();

        var request = new
        {
            Code = "TEST-002",
            Name = "Test Keyboard",
            Description = "Klavye test ürünü",
            Price = 500m,
            StockQuantity = 5,
            CategoryId = categoryId
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/products", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var location = createResponse.Headers.Location;
        Assert.NotNull(location);

        var getResponse = await client.GetAsync(location);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var product = await getResponse.Content
            .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(product);
        Assert.Equal("TEST-002", product.Code);
        Assert.Equal("Test Keyboard", product.Name);
        Assert.Equal(500m, product.Price);
        Assert.Equal(5, product.StockQuantity);
    }

    private sealed class ProductResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
    }

    [Fact]
    public async Task GetById_WhenProductDoesNotExist_ReturnsNotFound()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var nonExistingId = Guid.NewGuid();

        var response = await client.GetAsync(
            $"/api/products/{nonExistingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithDuplicateCode_ReturnsConflict()
    {
        await _factory.InitializeDatabaseAsync();
        var categoryId = await GetTestCategoryIdAsync();

        using var client = _factory.CreateClient();

        var request = new
        {
            Code = "DUPLICATE-001",
            Name = "İlk Ürün",
            Description = "İlk kayıt",
            Price = 100m,
            StockQuantity = 5,
            CategoryId = categoryId
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/products", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondRequest = new
        {
            Code = "DUPLICATE-001",
            Name = "İkinci Ürün",
            Description = "Tekrarlanan kod",
            Price = 200m,
            StockQuantity = 10,
            CategoryId = categoryId
        };

        var secondResponse = await client.PostAsJsonAsync(
            "/api/products", secondRequest);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithEmptyName_ReturnsBadRequest()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var request = new
        {
            Code = "TEST-INVALID-001",
            Name = "",
            Description = "Geçersiz ürün",
            Price = 100m,
            StockQuantity = 5
        };

        var response = await client.PostAsJsonAsync(
            "/api/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithNegativePrice_ReturnsBadRequest()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var request = new
        {
            Code = "TEST-INVALID-002",
            Name = "Geçersiz Fiyatlı Ürün",
            Description = "Negatif fiyat testi",
            Price = -100m,
            StockQuantity = 5
        };

        var response = await client.PostAsJsonAsync(
            "/api/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithNegativeStock_ReturnsBadRequest()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var request = new
        {
            Code = "TEST-INVALID-003",
            Name = "Negatif Stoklu Ürün",
            Description = "Negatif stok testi",
            Price = 100m,
            StockQuantity = -5
        };

        var response = await client.PostAsJsonAsync(
            "/api/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
