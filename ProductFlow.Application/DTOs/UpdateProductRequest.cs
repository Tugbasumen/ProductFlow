using System.ComponentModel.DataAnnotations;

namespace ProductFlow.Application.DTOs;

public sealed class UpdateProductRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [Range(0, 9999999999999999.99)]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int? MinimumStockLevel { get; init; }
}