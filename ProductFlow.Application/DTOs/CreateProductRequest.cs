using System;
using System.ComponentModel.DataAnnotations;

namespace ProductFlow.Application.DTOs;

public sealed class CreateProductRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; init; }

    [Range(0, int.MaxValue)]
    public int MinimumStockLevel { get; init; } = 5;

    [Required]
    public Guid CategoryId { get; init; }
}