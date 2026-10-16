using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ProductFlow.Application.DTOs;
using ProductFlow.Infrastructure.Persistence;

namespace ProductFlow.UnitTests;

public sealed class AuthenticationApiTests : IClassFixture<ProductApiFactory>
{
    private readonly ProductApiFactory _factory;

    public AuthenticationApiTests(ProductApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtWithRoleClaim()
    {
        await _factory.InitializeDatabaseAsync();
        const string password = "Valid-Test-Password-123!";
        var email = $"auth-{Guid.NewGuid():N}@example.test";

        using (var scope = _factory.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));

            var user = new ApplicationUser { UserName = email, Email = email };
            var createResult = await userManager.CreateAsync(user, password);
            Assert.True(createResult.Succeeded);
            var roleResult = await userManager.AddToRoleAsync(user, "Admin");
            Assert.True(roleResult.Succeeded);
        }

        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.Contains("Admin", result.Roles);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Equal("ProductFlow", jwt.Issuer);
        Assert.Contains("ProductFlow.Client", jwt.Audiences);
        Assert.Contains(jwt.Claims, claim => claim.Value == "Admin");
    }

    [Fact]
    public async Task CreateUser_WithAdminRole_CreatesOperatorAccount()
    {
        await _factory.InitializeDatabaseAsync();
        using var client = _factory.CreateClient();
        var email = $"operator-{Guid.NewGuid():N}@example.test";

        var response = await client.PostAsJsonAsync("/api/auth/users", new
        {
            Email = email,
            DisplayName = "Test Operator",
            Password = "Valid-Test-Password-123!",
            Role = "Operator"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var createdUserResponse = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, createdUserResponse.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user, "Operator"));
    }

    [Fact]
    public async Task Products_WithoutBearerToken_ReturnsUnauthorized()
    {
        await _factory.InitializeDatabaseAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithOperatorRole_ReturnsForbidden()
    {
        await _factory.InitializeDatabaseAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-operator");

        var response = await client.PostAsJsonAsync("/api/auth/users", new
        {
            Email = "blocked@example.test",
            DisplayName = "Blocked Operator",
            Password = "Valid-Test-Password-123!",
            Role = "Operator"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}