namespace ProductFlow.Application.DTOs;

public sealed class UpdateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
}