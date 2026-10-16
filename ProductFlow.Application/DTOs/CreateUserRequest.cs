using System.ComponentModel.DataAnnotations;

namespace ProductFlow.Application.DTOs;

public sealed class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [MinLength(12)]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string Role { get; init; } = "Operator";
}