using Microsoft.EntityFrameworkCore;
using ProductFlow.Application.DTOs;
using ProductFlow.Application.Interfaces;
using ProductFlow.Domain.Entities;
using ProductFlow.Infrastructure.Persistence;

namespace ProductFlow.Infrastructure.Services;

public sealed class OrderService : IOrderService
{
    private readonly ProductFlowDbContext _dbContext;

    public OrderService(ProductFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderResponse?> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ArgumentException(
                "Sipariş en az bir kalem içermelidir.",
                nameof(request));
        }

        var supportsTransactions = _dbContext.Database.IsRelational();

        await using var transaction = supportsTransactions
            ? await _dbContext.Database.BeginTransactionAsync(
                cancellationToken)
            : null;

        try
        {
            var customer = await _dbContext.Customers
                .FirstOrDefaultAsync(
                    existing => existing.Id == request.CustomerId,
                    cancellationToken);

            if (customer is null)
            {
                if (transaction is not null)
                    await transaction.RollbackAsync(cancellationToken);

                return null;
            }

            if (!customer.IsActive)
            {
                throw new InvalidOperationException(
                    "Pasif müşteri için sipariş oluşturulamaz.");
            }

            var groupedItems = request.Items
                .GroupBy(item => item.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(item => item.Quantity)
                })
                .ToList();

            var productIds = groupedItems
                .Select(item => item.ProductId)
                .ToList();

            var products = await _dbContext.Products
                .Where(product => productIds.Contains(product.Id))
                .ToListAsync(cancellationToken);

            if (products.Count != productIds.Count)
            {
                throw new InvalidOperationException(
                    "Siparişteki ürünlerden biri bulunamadı.");
            }

            var order = new Order(customer.Id);

            foreach (var item in groupedItems)
            {
                var product = products.Single(p => p.Id == item.ProductId);

                if (!product.IsActive)
                {
                    throw new InvalidOperationException(
                        $"Pasif ürün sipariş edilemez: {product.Code}");
                }

                product.AdjustStock(-item.Quantity);
                order.AddItem(product.Id, item.Quantity, product.Price);

                _dbContext.StockMovements.Add(new StockMovement(
                    product.Id,
                    StockMovementType.Out,
                    item.Quantity,
                    $"Sipariş {order.Id}"));
            }

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return await MapOrderAsync(order.Id, cancellationToken);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            throw;
        }
    }

    public Task<OrderResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return MapOrderAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetAllAsync(
        Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = OrderQuery();

        if (customerId.HasValue)
        {
            query = query.Where(order => order.CustomerId == customerId.Value);
        }

        var orders = await query
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenByDescending(order => order.Id)
            .ToListAsync(cancellationToken);

        return orders.Select(MapOrder).ToList();
    }

    public async Task<OrderResponse?> ConfirmAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(existing => existing.Items)
            .FirstOrDefaultAsync(
                existing => existing.Id == id,
                cancellationToken);

        if (order is null)
            return null;

        order.Confirm();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapOrderAsync(order.Id, cancellationToken);
    }

    public async Task<OrderResponse?> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var supportsTransactions = _dbContext.Database.IsRelational();

        await using var transaction = supportsTransactions
            ? await _dbContext.Database.BeginTransactionAsync(
                cancellationToken)
            : null;

        try
        {
            var order = await _dbContext.Orders
                .Include(existing => existing.Items)
                .FirstOrDefaultAsync(
                    existing => existing.Id == id,
                    cancellationToken);

            if (order is null)
            {
                if (transaction is not null)
                    await transaction.RollbackAsync(cancellationToken);

                return null;
            }

            order.Cancel();

            var productIds = order.Items
                .Select(item => item.ProductId)
                .Distinct()
                .ToList();

            var products = await _dbContext.Products
                .Where(product => productIds.Contains(product.Id))
                .ToListAsync(cancellationToken);

            foreach (var item in order.Items)
            {
                var product = products.Single(p => p.Id == item.ProductId);
                product.AdjustStock(item.Quantity);

                _dbContext.StockMovements.Add(new StockMovement(
                    product.Id,
                    StockMovementType.In,
                    item.Quantity,
                    $"Sipariş iptali {order.Id}"));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return await MapOrderAsync(order.Id, cancellationToken);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            throw;
        }
    }

    private IQueryable<Order> OrderQuery()
    {
        return _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.Items)
            .ThenInclude(item => item.Product);
    }

    private async Task<OrderResponse?> MapOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await OrderQuery()
            .FirstOrDefaultAsync(
                existing => existing.Id == orderId,
                cancellationToken);

        return order is null ? null : MapOrder(order);
    }

    private static OrderResponse MapOrder(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer.Name,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CreatedAtUtc = order.CreatedAtUtc,
            ConfirmedAtUtc = order.ConfirmedAtUtc,
            CancelledAtUtc = order.CancelledAtUtc,
            Items = order.Items
                .Select(item => new OrderItemResponse
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductCode = item.Product.Code,
                    ProductName = item.Product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.LineTotal
                })
                .ToList()
        };
    }
}
