using System.ComponentModel.DataAnnotations;

namespace ProductFlow.Application.DTOs;

public sealed class CreateCustomerRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; init; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; init; }
}
