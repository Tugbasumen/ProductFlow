
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using ProductFlow.Domain.Entities;
using ProductFlow.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Net.Http.Headers;

namespace ProductFlow.UnitTests;

public sealed class ProductApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public ProductApiFactory()
    {
        Environment.SetEnvironmentVariable(
            "Jwt__SigningKey",
            "integration-test-signing-key-must-be-at-least-32-bytes");
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "integration-test-signing-key-must-be-at-least-32-bytes",
            });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Error);
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestAdmin";
                options.DefaultChallengeScheme = "TestAdmin";
            }).AddScheme<AuthenticationSchemeOptions, TestAdminAuthenticationHandler>(
                "TestAdmin", _ => { });

            services.RemoveAll<DbContextOptions<ProductFlowDbContext>>();
            services.RemoveAll<ProductFlowDbContext>();

            services.AddDbContext<ProductFlowDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-admin");
    }

    private sealed class TestAdminAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAdminAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var token = Request.Headers.Authorization.ToString();
            if (token is not "Bearer test-admin" and not "Bearer test-operator")
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var role = token == "Bearer test-admin" ? "Admin" : "Operator";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "integration-admin"),
                new Claim(ClaimTypes.Name, "integration-admin"),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }


    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ProductFlowDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        var existingOrders = await dbContext.Orders.ToListAsync();
        dbContext.Orders.RemoveRange(existingOrders);

        var existingCustomers = await dbContext.Customers.ToListAsync();
        dbContext.Customers.RemoveRange(existingCustomers);

        var existingMovements = await dbContext.StockMovements.ToListAsync();
        dbContext.StockMovements.RemoveRange(existingMovements);

        var existingProducts = await dbContext.Products.ToListAsync();
        dbContext.Products.RemoveRange(existingProducts);

        // Kategorileri de temizle.
        var existingCategories = await dbContext.Categories.ToListAsync();
        dbContext.Categories.RemoveRange(existingCategories);

        await dbContext.SaveChangesAsync();

        // Her test için kategoriyi yeniden oluştur.
        dbContext.Categories.Add(
            new Category("Test Kategorisi", "API testleri için"));

        await dbContext.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
