using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ProductFlow.Application.DTOs;
using ProductFlow.Infrastructure.Persistence;

namespace ProductFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private static readonly string[] AllowedRoles = ["Admin", "Manager", "Operator"];
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { message = "E-posta veya parola hatalı." });

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);
        if (!signInResult.Succeeded)
            return Unauthorized(new { message = "E-posta veya parola hatalı." });

        var roles = await _userManager.GetRolesAsync(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email!)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:SigningKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "ProductFlow",
            audience: _configuration["Jwt:Audience"] ?? "ProductFlow.Client",
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return Ok(new AuthResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAt,
            Email = user.Email!,
            Roles = roles.ToArray()
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            email = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Email),
            roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value)
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!AllowedRoles.Contains(request.Role, StringComparer.Ordinal))
        {
            return BadRequest(new
            {
                message = "Rol Admin, Manager veya Operator olmalıdır."
            });
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            var roleResult = await _roleManager.CreateAsync(
                new IdentityRole<Guid>(request.Role));
            if (!roleResult.Succeeded)
                return Problem("Kullanıcı rolü oluşturulamadı.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
            EmailConfirmed = true
        };
        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = createResult.Errors.Select(error => error.Description)
            });
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!addRoleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return Problem("Kullanıcı rolü atanamadı.");
        }

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new
        {
            user.Id,
            user.Email,
            user.DisplayName,
            role = request.Role
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return NotFound();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.DisplayName,
            roles = await _userManager.GetRolesAsync(user)
        });
    }
}