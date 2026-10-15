using System.ComponentModel.DataAnnotations;

namespace ProductFlow.Application.DTOs;

public sealed class CreateOrderRequest
{
    [Required]
    public Guid CustomerId { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyList<CreateOrderItemRequest> Items { get; init; } = [];
}

public sealed class CreateOrderItemRequest
{
    [Required]
    public Guid ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}
