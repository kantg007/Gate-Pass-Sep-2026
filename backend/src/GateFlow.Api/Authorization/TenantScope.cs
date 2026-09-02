using System.Security.Claims;
using GateFlow.Application.Security;
using GateFlow.Domain.Entities;
using GateFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Authorization;

/// <summary>Shared tenant / site scoping for dashboard &amp; list endpoints.</summary>
public static class TenantScope
{
    public const int DeviceOfflineSeconds = 300;

    public static IQueryable<Site> Sites(GateFlowDbContext db, ClaimsPrincipal user, string? clientId = null)
    {
        var q = db.Sites.AsNoTracking().AsQueryable();
        if (CurrentUser.IsPlatformAdmin(user))
        {
            if (!string.IsNullOrWhiteSpace(clientId))
                q = q.Where(s => s.ClientId == clientId);
            return q;
        }

        var scopedClient = CurrentUser.ClientId(user);
        if (scopedClient is null) return q.Where(_ => false);
        q = q.Where(s => s.ClientId == scopedClient);
        var siteScope = CurrentUser.SiteId(user);
        if (!string.IsNullOrEmpty(siteScope))
            q = q.Where(s => s.Id == siteScope);
        return q;
    }

    public static IQueryable<AccessEvent> Events(
        GateFlowDbContext db,
        ClaimsPrincipal user,
        string? clientId = null,
        string? siteId = null)
    {
        var q = db.AccessEvents.AsNoTracking().AsQueryable();
        if (CurrentUser.IsPlatformAdmin(user))
        {
            if (!string.IsNullOrWhiteSpace(clientId))
                q = q.Where(e => e.ClientId == clientId);
        }
        else
        {
            var scopedClient = CurrentUser.ClientId(user);
            if (scopedClient is null) return q.Where(_ => false);
            q = q.Where(e => e.ClientId == scopedClient);
            var siteScope = CurrentUser.SiteId(user);
            if (!string.IsNullOrEmpty(siteScope))
                q = q.Where(e => e.SiteId == siteScope);
        }

        if (!string.IsNullOrWhiteSpace(siteId))
            q = q.Where(e => e.SiteId == siteId);
        return q;
    }

    public static IQueryable<Lane> Gates(
        GateFlowDbContext db,
        ClaimsPrincipal user,
        string? clientId = null,
        string? siteId = null)
    {
        var siteIds = Sites(db, user, clientId).Select(s => s.Id);
        var q = db.Lanes.AsNoTracking().Where(g => siteIds.Contains(g.SiteId));
        if (!string.IsNullOrWhiteSpace(siteId))
            q = q.Where(g => g.SiteId == siteId);
        return q;
    }

    public static IQueryable<HardwareDevice> Devices(
        GateFlowDbContext db,
        ClaimsPrincipal user,
        string? clientId = null,
        string? siteId = null)
    {
        var siteIds = Sites(db, user, clientId).Select(s => s.Id);
        var q = db.HardwareDevices.AsNoTracking().Where(d => siteIds.Contains(d.SiteId));
        if (!string.IsNullOrWhiteSpace(siteId))
            q = q.Where(d => d.SiteId == siteId);
        return q;
    }

    public static IQueryable<Alert> Alerts(
        GateFlowDbContext db,
        ClaimsPrincipal user,
        string? clientId = null,
        string? siteId = null)
    {
        var q = db.Alerts.AsNoTracking().AsQueryable();
        if (CurrentUser.IsPlatformAdmin(user))
        {
            if (!string.IsNullOrWhiteSpace(clientId))
                q = q.Where(a => a.ClientId == clientId);
        }
        else
        {
            var scopedClient = CurrentUser.ClientId(user);
            if (scopedClient is null) return q.Where(_ => false);
            q = q.Where(a => a.ClientId == scopedClient);
            var siteScope = CurrentUser.SiteId(user);
            if (!string.IsNullOrEmpty(siteScope))
                q = q.Where(a => a.SiteId == null || a.SiteId == siteScope);
        }

        if (!string.IsNullOrWhiteSpace(siteId))
            q = q.Where(a => a.SiteId == siteId);
        return q;
    }

    public static bool IsDeviceOnline(HardwareDevice? device, DateTime utcNow)
    {
        if (device is null) return false;
        if (device.ConnectionStatus == HardwareStatuses.Offline) return false;
        if (device.LastSeenAt is null) return false;
        return device.LastSeenAt >= utcNow.AddSeconds(-DeviceOfflineSeconds);
    }

    public static string ResolveGateStatus(Lane gate, IEnumerable<HardwareDevice> devices, DateTime utcNow)
    {
        var gateDevices = devices.Where(d => d.GateId == gate.Id).ToList();
        var anyOnline = gateDevices.Count == 0
            ? false
            : gateDevices.Any(d => IsDeviceOnline(d, utcNow));

        // No devices registered: treat active gates as reachable via lane key, offline only if inactive.
        if (gateDevices.Count == 0)
        {
            if (!gate.IsActive) return "OFFLINE";
            return gate.BarrierState == BarrierStates.Open ? "OPEN" : "CLOSED";
        }

        if (!anyOnline) return "OFFLINE";
        return gate.BarrierState == BarrierStates.Open ? "OPEN" : "CLOSED";
    }

    public static double? PctChange(double current, double previous)
    {
        if (previous <= 0) return current > 0 ? 100 : 0;
        return Math.Round((current - previous) / previous * 100, 1);
    }
}
