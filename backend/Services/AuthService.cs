using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GateFlow.Api.Data;
using GateFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GateFlow.Api.Services;

public class AuthService
{
    private readonly GateFlowDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(GateFlowDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<(bool Ok, string? Error, object? Payload)> RegisterClientAsync(
        string companyName,
        string fullName,
        string email,
        string password,
        string? phone)
    {
        email = email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return (false, "EMAIL_EXISTS", null);
        }

        var client = new Client
        {
            Name = companyName.Trim(),
            ContactEmail = email,
            Phone = phone,
            Status = "Active",
        };
        var user = new AppUser
        {
            Email = email,
            FullName = fullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = Roles.ClientAdmin,
            ClientId = client.Id,
        };

        _db.Clients.Add(client);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = CreateToken(user);
        return (true, null, AuthResponse(user, client, token));
    }

    public async Task<(bool Ok, string? Error, object? Payload)> LoginAsync(string email, string password)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Client).FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return (false, "INVALID_CREDENTIALS", null);
        }

        if (user.Client is not null && user.Client.Status == "Suspended")
        {
            return (false, "CLIENT_SUSPENDED", null);
        }

        var token = CreateToken(user);
        return (true, null, AuthResponse(user, user.Client, token));
    }

    private string CreateToken(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey()));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("role", user.Role),
            new("name", user.FullName),
        };
        if (!string.IsNullOrEmpty(user.ClientId))
        {
            claims.Add(new Claim("clientId", user.ClientId));
        }
        if (!string.IsNullOrEmpty(user.SiteId))
        {
            claims.Add(new Claim("siteId", user.SiteId));
        }

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "gateflow",
            audience: _config["Jwt:Audience"] ?? "gateflow",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string JwtKey() =>
        _config["Jwt:Key"] ?? "GateFlowDevSecretKey_ChangeMe_32chars!!";

    private static object AuthResponse(AppUser user, Client? client, string token) => new
    {
        token,
        user = new
        {
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            user.ClientId,
            user.SiteId,
            client = client == null ? null : new { client.Id, client.Name, client.Status },
        },
    };
}

public static class CurrentUser
{
    public static string? UserId(ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? Role(ClaimsPrincipal user) =>
        user.FindFirstValue("role") ?? user.FindFirstValue(ClaimTypes.Role);

    public static string? ClientId(ClaimsPrincipal user) => user.FindFirstValue("clientId");

    public static string? SiteId(ClaimsPrincipal user) => user.FindFirstValue("siteId");

    public static bool IsPlatformAdmin(ClaimsPrincipal user) => Role(user) == Roles.PlatformAdmin;
}
