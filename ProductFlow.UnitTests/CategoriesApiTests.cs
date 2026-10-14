
using System.Net;
using System.Net.Http.Json;

namespace ProductFlow.UnitTests;

public class CategoriesApiTests : IClassFixture<ProductApiFactory>
{
    private readonly ProductApiFactory _factory;

    public CategoriesApiTests(ProductApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithValidRequest_ReturnsCreated()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var request = new
        {
            Name = $"Elektronik-{Guid.NewGuid():N}",
            Description = "Elektronik ürünler"
        };

        var response = await client.PostAsJsonAsync(
            "/api/categories", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task GetById_WhenCategoryExists_ReturnsCategory()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var request = new
        {
            Name = $"Giyim-{Guid.NewGuid():N}",
            Description = "Giyim ürünleri"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/categories", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        var getResponse = await client.GetAsync(
            createResponse.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var category = await getResponse.Content
            .ReadFromJsonAsync<CategoryResponse>();

        Assert.NotNull(category);
        Assert.Equal(request.Name, category.Name);
        Assert.Equal(request.Description, category.Description);
    }

    [Fact]
    public async Task GetById_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/categories/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateName_ReturnsConflict()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var request = new
        {
            Name = $"Tekrarlı-{Guid.NewGuid():N}",
            Description = "Kategori testi"
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/categories", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync(
            "/api/categories", request);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithEmptyName_ReturnsBadRequest()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var request = new
        {
            Name = "",
            Description = "Geçersiz kategori"
        };

        var response = await client.PostAsJsonAsync(
            "/api/categories", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_WhenCategoryExists_ReturnsNoContent()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var createRequest = new
        {
            Name = $"Eski-{Guid.NewGuid():N}",
            Description = "Eski açıklama"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/categories", createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        var location = createResponse.Headers.Location;
        var id = Guid.Parse(location!.Segments[^1]);

        var updateRequest = new
        {
            Name = $"Yeni-{Guid.NewGuid():N}",
            Description = "Yeni açıklama"
        };

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/categories/{id}", updateRequest);

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/categories/{id}");
        var category = await getResponse.Content
            .ReadFromJsonAsync<CategoryResponse>();

        Assert.NotNull(category);
        Assert.Equal(updateRequest.Name, category.Name);
        Assert.Equal(updateRequest.Description, category.Description);
    }

    [Fact]
    public async Task DeactivateCategory_WhenCategoryExists_ReturnsNoContent()
    {
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        var request = new
        {
            Name = $"Pasif-{Guid.NewGuid():N}",
            Description = "Pasifleştirme testi"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/categories", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        var location = createResponse.Headers.Location;
        var id = Guid.Parse(location!.Segments[^1]);

        var deactivateResponse = await client.PatchAsync(
            $"/api/categories/{id}/deactivate", content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            deactivateResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/categories/{id}");
        var category = await getResponse.Content
            .ReadFromJsonAsync<CategoryResponse>();

        Assert.NotNull(category);
        Assert.False(category.IsActive);
    }

    private sealed class CategoryResponse
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
