using Microsoft.EntityFrameworkCore;
using ProductFlow.Application.DTOs;
using ProductFlow.Application.Interfaces;
using ProductFlow.Domain.Entities;
using ProductFlow.Infrastructure.Persistence;

namespace ProductFlow.Infrastructure.Services;

public sealed class StockMovementService : IStockMovementService
{
    private readonly ProductFlowDbContext _dbContext;

    public StockMovementService(ProductFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StockMovementResponse?> CreateAsync(
        CreateStockMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Quantity),
                "Stok hareketi miktarı sıfırdan büyük olmalıdır.");
        }

        if (request.Type is not StockMovementType.In
            and not StockMovementType.Out)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Type),
                "Geçersiz stok hareketi türü.");
        }

        var supportsTransactions =
            _dbContext.Database.IsRelational();

        await using var transaction = supportsTransactions
            ? await _dbContext.Database.BeginTransactionAsync(
                cancellationToken)
            : null;

        try
        {
            var product = await _dbContext.Products
                .FirstOrDefaultAsync(
                    p => p.Id == request.ProductId,
                    cancellationToken);

            if (product is null)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return null;
            }

            var quantityChange = request.Type == StockMovementType.In
                ? request.Quantity
                : -request.Quantity;

            product.AdjustStock(quantityChange);

            var movement = new StockMovement(
                product.Id,
                request.Type,
                request.Quantity,
                request.Description);

            _dbContext.StockMovements.Add(movement);

            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return await MapMovementAsync(
                movement.Id,
                cancellationToken);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);
            }

            throw;
        }
    }

    public async Task<IReadOnlyList<StockMovementResponse>> GetAllAsync(
        Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.StockMovements
            .AsNoTracking()
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(
                m => m.ProductId == productId.Value);
        }

        return await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .ThenByDescending(m => m.Id)
            .Select(m => new StockMovementResponse
            {
                Id = m.Id,
                ProductId = m.ProductId,
                ProductCode = m.Product.Code,
                ProductName = m.Product.Name,
                Type = m.Type,
                Quantity = m.Quantity,
                Description = m.Description,
                CreatedAtUtc = m.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<StockMovementResponse> MapMovementAsync(
        Guid movementId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.StockMovements
            .AsNoTracking()
            .Where(m => m.Id == movementId)
            .Select(m => new StockMovementResponse
            {
                Id = m.Id,
                ProductId = m.ProductId,
                ProductCode = m.Product.Code,
                ProductName = m.Product.Name,
                Type = m.Type,
                Quantity = m.Quantity,
                Description = m.Description,
                CreatedAtUtc = m.CreatedAtUtc
            })
            .SingleAsync(cancellationToken);
    }
}