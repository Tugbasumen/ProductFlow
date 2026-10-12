using System.ComponentModel.DataAnnotations;

namespace ProductFlow.Application.DTOs;

public sealed class AdjustStockRequest
{
    [Required]
    public int QuantityChange { get; init; }
}