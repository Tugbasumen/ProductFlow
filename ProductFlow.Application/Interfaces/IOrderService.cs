using ProductFlow.Application.DTOs;

namespace ProductFlow.Application.Interfaces;

public interface IOrderService
{
    Task<OrderResponse?> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderResponse>> GetAllAsync(
        Guid? customerId = null,
        CancellationToken cancellationToken = default);

    Task<OrderResponse?> ConfirmAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OrderResponse?> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
