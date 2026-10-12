namespace ProductFlow.Application.DTOs;

public sealed class CreateProductRequest
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public int StockQuantity { get; init; }
}