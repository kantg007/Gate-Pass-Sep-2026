using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GateFlow.Application.Abstractions;
using GateFlow.Contracts.Auth;
using GateFlow.Domain.Entities;
using GateFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GateFlow.Infrastructure.Handlers;

public sealed class AuthHandler : IAuthService
{
    private readonly GateFlowDbContext _db;
    private readonly IConfiguration _config;

    public AuthHandler(GateFlowDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<(bool Ok, string? Error, AuthResponse? Payload)> RegisterClientAsync(
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

        var starter = await _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Code == "STARTER");
        if (starter is not null)
        {
            _db.Subscriptions.Add(new Subscription
            {
                ClientId = client.Id,
                PlanId = starter.Id,
                Status = "Trial",
                StartsAt = DateTime.UtcNow,
                EndsAt = DateTime.UtcNow.AddDays(14),
                GraceEndsAt = DateTime.UtcNow.AddDays(21),
            });
        }

        await _db.SaveChangesAsync();
        var adminRole = await SeedData.EnsureClientDefaultRolesAsync(_db, client.Id);
        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
        _db.AuditLogs.Add(new AuditLog
        {
            ClientId = client.Id,
            ActorUserId = user.Id,
            Action = "CLIENT_REGISTER",
            EntityType = "Client",
            EntityId = client.Id,
            Summary = $"Self-registered client {client.Name}",
        });
        await _db.SaveChangesAsync();

        var token = CreateToken(user);
        return (true, null, ToAuthResponse(user, client, token));
    }

    public async Task<(bool Ok, string? Error, AuthResponse? Payload)> LoginAsync(string email, string password)
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
        return (true, null, ToAuthResponse(user, user.Client, token));
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

    private static AuthResponse ToAuthResponse(AppUser user, Client? client, string token) =>
        new(
            token,
            new AuthUserDto(
                user.Id,
                user.Email,
                user.FullName,
                user.Role,
                user.ClientId,
                user.SiteId,
                client == null ? null : new ClientSummaryDto(client.Id, client.Name, client.Status)));
}
