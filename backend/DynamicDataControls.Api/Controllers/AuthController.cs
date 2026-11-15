using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DynamicDataControls.Api.Models.Domain;

namespace DynamicDataControls.Api.Controllers;

/// <summary>
/// Dev/testing-only token endpoint — NOT for production use.
/// Issues a signed JWT for any username + one of the known roles, with no real identity check.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────
    // DTOs
    // ─────────────────────────────────────────────────────────────

    /// <param name="Username">Any non-empty display name (no real auth performed).</param>
    /// <param name="Password">Ignored — present only to match a typical login payload shape.</param>
    /// <param name="Role">Must be one of the values defined in <see cref="Roles"/>.</param>
    public sealed record TokenRequest(string Username, string Password, string Role);

    public sealed record TokenResponse(string Token, string Username, string Role, DateTime ExpiresAt);

    // ─────────────────────────────────────────────────────────────
    // POST /api/auth/token
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Issues a development JWT.
    /// Provide a username and one of the supported roles; password is not validated.
    /// </summary>
    /// <remarks>
    /// ⚠️  FOR LOCAL DEVELOPMENT AND TESTING ONLY.
    /// Remove or gate this endpoint behind an environment check before deploying to production.
    /// </remarks>
    [HttpPost("token")]
    public IActionResult GenerateToken([FromBody] TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new { error = "שם משתמש הוא שדה חובה" });

        var knownRoles = new[] { Roles.ControlsManager, Roles.SystemAdmin };
        if (!knownRoles.Contains(request.Role))
            return BadRequest(new
            {
                error = $"תפקיד לא מוכר. תפקידים תקינים: {string.Join(", ", knownRoles)}"
            });

        var jwtSection  = _configuration.GetSection("Jwt");
        var secretKey   = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
        var issuer      = jwtSection["Issuer"]   ?? "DynamicDataControls";
        var audience    = jwtSection["Audience"] ?? "DynamicDataControls";
        var expiryHours = int.TryParse(jwtSection["ExpiryHours"], out var h) ? h : 8;

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt   = DateTime.UtcNow.AddHours(expiryHours);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name,                 request.Username),
            new Claim(ClaimTypes.Role,                 request.Role),
            new Claim(JwtRegisteredClaimNames.Sub,     request.Username),
            new Claim(JwtRegisteredClaimNames.Jti,     Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogInformation(
            "[DEV] Issued JWT for user {Username} with role {Role}, expires {ExpiresAt}",
            request.Username, request.Role, expiresAt);

        return Ok(new TokenResponse(tokenString, request.Username, request.Role, expiresAt));
    }
}
