using System.Net;
using System.Net.Http.Json;

namespace ProductFlow.UnitTests;

public class CustomersApiTests : IClassFixture<ProductApiFactory>
{
    private readonly ProductApiFactory _factory;

    public CustomersApiTests(ProductApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateCustomer_WithValidRequest_ReturnsCreated()
    {
        await _factory.InitializeDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/customers", new
        {
            Name = "Ayşe Yılmaz",
            Email = $"ayse-{Guid.NewGuid():N}@example.com",
            Phone = "5551112233"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateCustomer_WithDuplicateEmail_ReturnsConflict()
    {
        await _factory.InitializeDatabaseAsync();
        using var client = _factory.CreateClient();

        var email = $"dup-{Guid.NewGuid():N}@example.com";

        var first = await client.PostAsJsonAsync("/api/customers", new
        {
            Name = "Ayşe Yılmaz",
            Email = email
        });

        var second = await client.PostAsJsonAsync("/api/customers", new
        {
            Name = "Ayşe Kaya",
            Email = email
        });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenCustomerExists_ReturnsCustomer()
    {
        await _factory.InitializeDatabaseAsync();
        using var client = _factory.CreateClient();

        var email = $"get-{Guid.NewGuid():N}@example.com";
        var createResponse = await client.PostAsJsonAsync("/api/customers", new
        {
            Name = "Ali Veli",
            Email = email
        });

        var customer = await createResponse.Content
            .ReadFromJsonAsync<CustomerResponse>();

        var getResponse = await client.GetAsync(
            $"/api/customers/{customer!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content
            .ReadFromJsonAsync<CustomerResponse>();

        Assert.Equal("Ali Veli", fetched!.Name);
        Assert.Equal(email.ToLowerInvariant(), fetched.Email);
    }

    private sealed class CustomerResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}
