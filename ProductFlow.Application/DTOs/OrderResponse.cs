using ProductFlow.Domain.Entities;

namespace ProductFlow.Application.DTOs;

public sealed class OrderResponse
{
    public Guid Id { get; init; }

    public Guid CustomerId { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public OrderStatus Status { get; init; }

    public decimal TotalAmount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? ConfirmedAtUtc { get; init; }

    public DateTime? CancelledAtUtc { get; init; }

    public IReadOnlyList<OrderItemResponse> Items { get; init; } = [];
}

public sealed class OrderItemResponse
{
    public Guid Id { get; init; }

    public Guid ProductId { get; init; }

    public string ProductCode { get; init; } = string.Empty;

    public string ProductName { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal { get; init; }
}
