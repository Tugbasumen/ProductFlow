using Microsoft.EntityFrameworkCore;
using ProductFlow.Application.DTOs;
using ProductFlow.Domain.Entities;
using ProductFlow.Infrastructure.Persistence;
using ProductFlow.Infrastructure.Services;

namespace ProductFlow.UnitTests;

public class OrderServiceTests
{
    private static ProductFlowDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProductFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ProductFlowDbContext(options);
    }

    private static async Task<(Customer Customer, Product Product)> SeedAsync(
        ProductFlowDbContext dbContext,
        int stockQuantity = 10)
    {
        var category = new Category("Sipariş Kategorisi");
        var product = new Product(
            "ORD-001", "Sipariş Ürünü", 150m, stockQuantity, category.Id);
        var customer = new Customer("Ali Veli", "ali@example.com");

        dbContext.Categories.Add(category);
        dbContext.Products.Add(product);
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        return (customer, product);
    }

    [Fact]
    public async Task CreateAsync_WhenCustomerDoesNotExist_ReturnsNull()
    {
        using var dbContext = CreateDbContext();
        var service = new OrderService(dbContext);

        var result = await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1
                }
            ]
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithValidItems_DecreasesStockAndCreatesMovement()
    {
        using var dbContext = CreateDbContext();
        var (customer, product) = await SeedAsync(dbContext);
        var service = new OrderService(dbContext);

        var result = await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = product.Id,
                    Quantity = 3
                }
            ]
        });

        Assert.NotNull(result);
        Assert.Equal(OrderStatus.Pending, result.Status);
        Assert.Equal(450m, result.TotalAmount);
        Assert.Equal(7, product.StockQuantity);
        Assert.Single(dbContext.StockMovements);
        Assert.Equal(StockMovementType.Out, dbContext.StockMovements.Single().Type);
    }

    [Fact]
    public async Task CreateAsync_WhenStockIsInsufficient_ThrowsException()
    {
        using var dbContext = CreateDbContext();
        var (customer, product) = await SeedAsync(dbContext, stockQuantity: 2);
        var service = new OrderService(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(new CreateOrderRequest
            {
                CustomerId = customer.Id,
                Items =
                [
                    new CreateOrderItemRequest
                    {
                        ProductId = product.Id,
                        Quantity = 5
                    }
                ]
            }));

        Assert.Equal(2, product.StockQuantity);
        Assert.Empty(dbContext.Orders);
        Assert.Empty(dbContext.StockMovements);
    }

    [Fact]
    public async Task ConfirmAsync_WhenOrderIsPending_ConfirmsOrder()
    {
        using var dbContext = CreateDbContext();
        var (customer, product) = await SeedAsync(dbContext);
        var service = new OrderService(dbContext);

        var created = await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = product.Id,
                    Quantity = 1
                }
            ]
        });

        var confirmed = await service.ConfirmAsync(created!.Id);

        Assert.NotNull(confirmed);
        Assert.Equal(OrderStatus.Confirmed, confirmed.Status);
        Assert.NotNull(confirmed.ConfirmedAtUtc);
    }

    [Fact]
    public async Task CancelAsync_RestoresStock()
    {
        using var dbContext = CreateDbContext();
        var (customer, product) = await SeedAsync(dbContext);
        var service = new OrderService(dbContext);

        var created = await service.CreateAsync(new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = product.Id,
                    Quantity = 4
                }
            ]
        });

        var cancelled = await service.CancelAsync(created!.Id);

        Assert.NotNull(cancelled);
        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
        Assert.Equal(10, product.StockQuantity);
        Assert.Equal(2, dbContext.StockMovements.Count());
    }
}
