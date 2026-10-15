
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductFlow.Domain.Entities;
using ProductFlow.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace ProductFlow.UnitTests;

public sealed class ProductApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public ProductApiFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Error);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ProductFlowDbContext>>();
            services.RemoveAll<ProductFlowDbContext>();

            services.AddDbContext<ProductFlowDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }


    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ProductFlowDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

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
