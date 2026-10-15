using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
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
    public async Task GetAll_WithCategoryId_ReturnsOnlyProductsInThatCategory()
    {
        await _factory.InitializeDatabaseAsync();

        var categoryId = await GetTestCategoryIdAsync();

        using var client = _factory.CreateClient();

        var firstRequest = new
        {
            Code = "FILTER-001",
            Name = "Kategori Filtre Mouse",
            Description = "Filtre testi",
            Price = 100m,
            StockQuantity = 5,
            CategoryId = categoryId
        };

        var secondRequest = new
        {
            Code = "FILTER-002",
            Name = "Kategori Filtre Klavye",
            Description = "Filtre testi",
            Price = 200m,
            StockQuantity = 10,
            CategoryId = categoryId
        };

        var firstCreateResponse = await client.PostAsJsonAsync(
            "/api/products", firstRequest);

        var secondCreateResponse = await client.PostAsJsonAsync(
            "/api/products", secondRequest);

        Assert.Equal(HttpStatusCode.Created, firstCreateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondCreateResponse.StatusCode);

        var response = await client.GetAsync(
            $"/api/products?categoryId={categoryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content
            .ReadFromJsonAsync<PagedProductResponse>();

        Assert.NotNull(products);
        Assert.Contains(products.Items, p => p.Code == "FILTER-001");
        Assert.Contains(products.Items, p => p.Code == "FILTER-002");
        Assert.All(
            products.Items,
            p => Assert.Equal(categoryId, p.CategoryId));

        Assert.Equal(2, products.TotalCount);
    }

    [Fact]
    public async Task GetAll_WithUnknownCategoryId_ReturnsEmptyList()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var unknownCategoryId = Guid.NewGuid();

        var response = await client.GetAsync(
            $"/api/products?categoryId={unknownCategoryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content
            .ReadFromJsonAsync<PagedProductResponse>();

        Assert.NotNull(products);
        Assert.Empty(products.Items);
        Assert.Equal(0, products.TotalCount);
        Assert.Equal(0, products.TotalPages);
    }
    [Fact]
    public async Task GetAll_WithPageSize_ReturnsPaginationMetadata()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/products?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedProductResponse>();

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.True(result.Items.Count <= 2);
        Assert.True(result.TotalCount >= result.Items.Count);
        Assert.Equal(
            (int)Math.Ceiling((double)result.TotalCount / result.PageSize),
            result.TotalPages);
    }

    [Fact]
    public async Task GetAll_WithInvalidPage_ReturnsBadRequest()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/products?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task GetAll_WithSecondPage_ReturnsRequestedPage()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var firstResponse = await client.GetAsync(
            "/api/products?page=1&pageSize=1");

        var secondResponse = await client.GetAsync(
            "/api/products?page=2&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var firstPage = await firstResponse.Content
            .ReadFromJsonAsync<PagedProductResponse>();

        var secondPage = await secondResponse.Content
            .ReadFromJsonAsync<PagedProductResponse>();

        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);

        Assert.Equal(1, firstPage.Page);
        Assert.Equal(2, secondPage.Page);
        Assert.Equal(1, firstPage.PageSize);
        Assert.Equal(1, secondPage.PageSize);

        if (firstPage.TotalCount >= 2)
        {
            Assert.Single(firstPage.Items);
            Assert.Single(secondPage.Items);

            Assert.NotEqual(
                firstPage.Items[0].Code,
                secondPage.Items[0].Code);
        }
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

        var response = await client.PostAsJsonAsync(
            "/api/products", request);

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

    private sealed class FilteredProductResponse
    {
        public Guid CategoryId { get; set; }

        public string Code { get; set; } = string.Empty;
    }

    private sealed class PagedProductResponse
    {
        public List<FilteredProductResponse> Items { get; set; } = [];

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }

    private sealed class ProductResponse
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }
    }
}