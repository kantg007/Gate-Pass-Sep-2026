using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GateFlow.Domain.Entities;

namespace GateFlow.Application.Security;

public static class CurrentUser
{
    public static string? UserId(ClaimsPrincipal user) =>
        user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public static string? Role(ClaimsPrincipal user) =>
        user.FindFirst("role")?.Value
        ?? user.FindFirst(ClaimTypes.Role)?.Value;

    public static string? ClientId(ClaimsPrincipal user) =>
        user.FindFirst("clientId")?.Value;

    public static string? SiteId(ClaimsPrincipal user) =>
        user.FindFirst("siteId")?.Value;

    public static bool IsPlatformAdmin(ClaimsPrincipal user) => Role(user) == Roles.PlatformAdmin;
}
