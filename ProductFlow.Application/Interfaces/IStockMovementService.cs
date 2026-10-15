using ProductFlow.Application.DTOs;

namespace ProductFlow.Application.Interfaces;

public interface IStockMovementService
{
    Task<StockMovementResponse?> CreateAsync(
        CreateStockMovementRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovementResponse>> GetAllAsync(
        Guid? productId = null,
        CancellationToken cancellationToken = default);
}