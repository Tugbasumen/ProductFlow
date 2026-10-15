using Microsoft.EntityFrameworkCore;
using ProductFlow.Application.DTOs;
using ProductFlow.Infrastructure.Persistence;
using ProductFlow.Infrastructure.Services;

namespace ProductFlow.UnitTests;

public class CustomerServiceTests
{
    private static ProductFlowDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProductFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ProductFlowDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesCustomer()
    {
        using var dbContext = CreateDbContext();
        var service = new CustomerService(dbContext);

        var result = await service.CreateAsync(new CreateCustomerRequest
        {
            Name = "Ayşe Yılmaz",
            Email = "ayse@example.com",
            Phone = "5551112233"
        });

        Assert.Equal("Ayşe Yılmaz", result.Name);
        Assert.Equal("ayse@example.com", result.Email);
        Assert.True(result.IsActive);
        Assert.Single(dbContext.Customers);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsException()
    {
        using var dbContext = CreateDbContext();
        var service = new CustomerService(dbContext);

        await service.CreateAsync(new CreateCustomerRequest
        {
            Name = "Ayşe Yılmaz",
            Email = "ayse@example.com"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(new CreateCustomerRequest
            {
                Name = "Ayşe Kaya",
                Email = "AYSE@example.com"
            }));
    }

    [Fact]
    public async Task DeactivateAsync_WhenCustomerExists_DeactivatesCustomer()
    {
        using var dbContext = CreateDbContext();
        var service = new CustomerService(dbContext);

        var customer = await service.CreateAsync(new CreateCustomerRequest
        {
            Name = "Ayşe Yılmaz",
            Email = "ayse@example.com"
        });

        var result = await service.DeactivateAsync(customer.Id);

        Assert.True(result);
        Assert.False(customer.IsActive);
    }
}
