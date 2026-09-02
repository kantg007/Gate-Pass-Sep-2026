using System.Security.Claims;
using GateFlow.Application.Security;
using GateFlow.Domain.Entities;
using GateFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Authorization;

public static class SiteAccessGuard
{
    public static bool CanAccessSite(ClaimsPrincipal user, Site site)
    {
        if (CurrentUser.IsPlatformAdmin(user)) return true;
        var clientId = CurrentUser.ClientId(user);
        if (clientId is null || site.ClientId != clientId) return false;
        var siteScope = CurrentUser.SiteId(user);
        if (!string.IsNullOrEmpty(siteScope) && siteScope != site.Id) return false;
        return true;
    }

    public static async Task<bool> CanAccessSiteIdAsync(
        ClaimsPrincipal user,
        GateFlowDbContext db,
        string siteId,
        CancellationToken ct = default)
    {
        var site = await db.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == siteId, ct);
        return site is not null && CanAccessSite(user, site);
    }
}
