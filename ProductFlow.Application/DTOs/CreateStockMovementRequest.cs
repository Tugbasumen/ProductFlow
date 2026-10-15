using System.ComponentModel.DataAnnotations;
using ProductFlow.Domain.Entities;

namespace ProductFlow.Application.DTOs;

public sealed class CreateStockMovementRequest
{
    [Required]
    public Guid ProductId { get; init; }

    [EnumDataType(typeof(StockMovementType))]
    public StockMovementType Type { get; init; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }
}