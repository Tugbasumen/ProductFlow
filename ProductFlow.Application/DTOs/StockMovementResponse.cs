using ProductFlow.Domain.Entities;

namespace ProductFlow.Application.DTOs;

public sealed class StockMovementResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductCode { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public StockMovementType Type { get; init; }
    public int Quantity { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}