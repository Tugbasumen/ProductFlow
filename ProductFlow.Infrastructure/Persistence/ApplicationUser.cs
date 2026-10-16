using Microsoft.AspNetCore.Identity;

namespace ProductFlow.Infrastructure.Persistence;

public sealed class ApplicationUser : IdentityUser<Guid>
{
	public string DisplayName { get; set; } = string.Empty;
}