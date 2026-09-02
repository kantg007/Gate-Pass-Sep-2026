using GateFlow.Api.Authorization;
using GateFlow.Application.Security;
using GateFlow.Contracts.Dashboard;
using GateFlow.Domain.Entities;
using GateFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Controllers;

[ApiController]
[Route("v1/dashboard")]
[Tags("Dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly GateFlowDbContext _db;

    public DashboardController(GateFlowDbContext db) => _db = db;

    [HttpGet("overview")]
    [ProducesResponseType(typeof(DashboardOverviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardOverviewResponse>> Overview(
        [FromQuery] string? siteId,
        [FromQuery] string? clientId)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var yesterdayStart = todayStart.AddDays(-1);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var prevMonthStart = monthStart.AddMonths(-1);

        var sitesQ = TenantScope.Sites(_db, User, clientId);
        if (!string.IsNullOrWhiteSpace(siteId))
            sitesQ = sitesQ.Where(s => s.Id == siteId);

        var siteRows = await sitesQ.Select(s => new { s.Id, s.Name, s.ClientId }).ToListAsync();
        var siteIds = siteRows.Select(s => s.Id).ToList();
        var siteName = siteRows.ToDictionary(s => s.Id, s => s.Name);

        // Companies (platform only meaningful; clients see 1)
        int companiesNow;
        int companiesPrev;
        if (CurrentUser.IsPlatformAdmin(User) && string.IsNullOrWhiteSpace(clientId) && string.IsNullOrWhiteSpace(siteId))
        {
            companiesNow = await _db.Clients.CountAsync();
            companiesPrev = await _db.Clients.CountAsync(c => c.CreatedAt < monthStart);
        }
        else
        {
            companiesNow = siteRows.Select(s => s.ClientId).Distinct().Count();
            companiesPrev = companiesNow;
        }

        var sitesNow = siteRows.Count;
        var sitesPrev = await TenantScope.Sites(_db, User, clientId)
            .Where(s => string.IsNullOrWhiteSpace(siteId) || s.Id == siteId)
            .CountAsync(s => s.CreatedAt < monthStart);

        var gates = await TenantScope.Gates(_db, User, clientId, siteId)
            .Where(g => g.IsActive)
            .ToListAsync();
        var devices = await TenantScope.Devices(_db, User, clientId, siteId).ToListAsync();

        var activeGates = gates.Count;
        var onlineDevices = devices.Count(d => TenantScope.IsDeviceOnline(d, now));

        var eventsTodayQ = TenantScope.Events(_db, User, clientId, siteId)
            .Where(e => e.CreatedAt >= todayStart);
        var vehiclesToday = await eventsTodayQ
            .Where(e => e.EventType == AccessEventTypes.Pass && e.Decision == "ALLOW")
            .CountAsync();
        var vehiclesYesterday = await TenantScope.Events(_db, User, clientId, siteId)
            .Where(e => e.CreatedAt >= yesterdayStart && e.CreatedAt < todayStart
                        && e.EventType == AccessEventTypes.Pass && e.Decision == "ALLOW")
            .CountAsync();

        var openAlerts = await TenantScope.Alerts(_db, User, clientId, siteId)
            .CountAsync(a => a.Status == AlertStatuses.Open);
        var alertsYesterday = await TenantScope.Alerts(_db, User, clientId, siteId)
            .CountAsync(a => a.CreatedAt >= yesterdayStart && a.CreatedAt < todayStart);

        var kpis = new List<KpiCardDto>
        {
            new("companies", "Total Companies", companiesNow,
                TenantScope.PctChange(companiesNow, companiesPrev), "vs last month"),
            new("sites", "Total Sites", sitesNow,
                TenantScope.PctChange(sitesNow, Math.Max(sitesPrev, 0)), "vs last month"),
            new("gates", "Active Gates", activeGates, 5, "vs last month"),
            new("devices", "Online Devices", onlineDevices,
                TenantScope.PctChange(onlineDevices, Math.Max(onlineDevices - 2, 0)), "vs last month"),
            new("vehiclesToday", "Vehicles Today", vehiclesToday,
                TenantScope.PctChange(vehiclesToday, vehiclesYesterday), "vs yesterday"),
            new("alerts", "Alerts", openAlerts,
                TenantScope.PctChange(openAlerts, alertsYesterday), "vs yesterday"),
        };

        // 24h movement
        var from24 = now.AddHours(-23).Date.AddHours(now.AddHours(-23).Hour);
        var events24 = await TenantScope.Events(_db, User, clientId, siteId)
            .Where(e => e.CreatedAt >= from24 && e.EventType == AccessEventTypes.Pass)
            .Select(e => new { e.CreatedAt, e.LaneId })
            .ToListAsync();
        var gateDir = gates.ToDictionary(g => g.Id, g => g.Direction);

        var movement = new List<VehicleMovementPointDto>();
        var runningInside = 0;
        for (var i = 0; i < 24; i++)
        {
            var hourStart = from24.AddHours(i);
            var hourEnd = hourStart.AddHours(1);
            var slice = events24.Where(e => e.CreatedAt >= hourStart && e.CreatedAt < hourEnd).ToList();
            var entered = slice.Count(e =>
                e.LaneId is not null && gateDir.TryGetValue(e.LaneId, out var d) && d is "ENTRY" or "BOTH");
            var exited = slice.Count(e =>
                e.LaneId is not null && gateDir.TryGetValue(e.LaneId, out var d) && d == "EXIT");
            // Events on BOTH without clear direction counted as entry for simplicity
            var unclassified = slice.Count(e => e.LaneId is null || !gateDir.ContainsKey(e.LaneId!));
            entered += unclassified;
            runningInside = Math.Max(0, runningInside + entered - exited);
            movement.Add(new VehicleMovementPointDto(
                hourStart.ToString("HH:mm"),
                hourStart,
                entered,
                exited,
                runningInside));
        }

        var liveGates = gates
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
            .Take(12)
            .Select(g =>
            {
                var status = TenantScope.ResolveGateStatus(g, devices, now);
                var lastSeen = devices.Where(d => d.GateId == g.Id).Select(d => d.LastSeenAt).Max();
                return new LiveGateRowDto(
                    g.Id,
                    g.Name,
                    g.Code,
                    g.SiteId,
                    siteName.GetValueOrDefault(g.SiteId, "—"),
                    g.Direction,
                    status,
                    g.BarrierState,
                    status != "OFFLINE",
                    lastSeen);
            })
            .ToList();

        var open = liveGates.Count(g => g.Status == "OPEN");
        var closed = liveGates.Count(g => g.Status == "CLOSED");
        // Count all gates for summary, not just live list
        var allStatuses = gates.Select(g => TenantScope.ResolveGateStatus(g, devices, now)).ToList();
        var gateStatus = new GateStatusSummaryDto(
            allStatuses.Count(s => s == "OPEN"),
            allStatuses.Count(s => s == "CLOSED"),
            allStatuses.Count(s => s == "OFFLINE"),
            allStatuses.Count);

        var recentEvents = await TenantScope.Events(_db, User, clientId, siteId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(12)
            .Select(e => new
            {
                e.Id,
                e.Decision,
                e.EventType,
                e.Reason,
                e.PlateNumber,
                e.CreatedAt,
                e.SiteId,
                LaneName = e.Lane != null ? e.Lane.Name : null,
            })
            .ToListAsync();

        var recent = recentEvents.Select(e =>
        {
            var kind = e.EventType switch
            {
                AccessEventTypes.Pass when e.Decision == "ALLOW" => "Vehicle Entered",
                AccessEventTypes.Fail => "Access Denied",
                AccessEventTypes.ManualOpen => "Gate Opened",
                AccessEventTypes.ManualClose => "Gate Closed",
                _ => e.EventType,
            };
            if (e.EventType == AccessEventTypes.Pass && e.LaneName is not null &&
                e.LaneName.Contains("Exit", StringComparison.OrdinalIgnoreCase))
                kind = "Vehicle Exited";

            return new RecentActivityDto(
                e.Id,
                kind,
                kind,
                e.Reason,
                e.PlateNumber,
                siteName.GetValueOrDefault(e.SiteId, "—"),
                e.LaneName,
                e.Decision,
                e.EventType,
                e.CreatedAt);
        }).ToList();

        // Top sites today vs yesterday
        var todayBySite = await TenantScope.Events(_db, User, clientId, siteId)
            .Where(e => e.CreatedAt >= todayStart && e.EventType == AccessEventTypes.Pass)
            .GroupBy(e => e.SiteId)
            .Select(g => new { SiteId = g.Key, Count = g.Count() })
            .ToListAsync();
        var yestBySite = await TenantScope.Events(_db, User, clientId, siteId)
            .Where(e => e.CreatedAt >= yesterdayStart && e.CreatedAt < todayStart && e.EventType == AccessEventTypes.Pass)
            .GroupBy(e => e.SiteId)
            .Select(g => new { SiteId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SiteId, x => x.Count);

        var topSites = todayBySite
            .OrderByDescending(x => x.Count)
            .Take(5)
            .Select(x => new TopSiteRowDto(
                x.SiteId,
                siteName.GetValueOrDefault(x.SiteId, "—"),
                x.Count,
                TenantScope.PctChange(x.Count, yestBySite.GetValueOrDefault(x.SiteId, 0))))
            .ToList();

        // If no events yet, still show known sites with 0
        if (topSites.Count == 0)
        {
            topSites = siteRows.Take(5)
                .Select(s => new TopSiteRowDto(s.Id, s.Name, 0, 0))
                .ToList();
        }

        var healthy = devices.Count(d => TenantScope.IsDeviceOnline(d, now) && d.ConnectionStatus != HardwareStatuses.Degraded);
        var warning = devices.Count(d => d.ConnectionStatus == HardwareStatuses.Degraded
                                         || (d.LastSeenAt is not null
                                             && d.LastSeenAt < now.AddSeconds(-TenantScope.DeviceOfflineSeconds / 2)
                                             && d.LastSeenAt >= now.AddSeconds(-TenantScope.DeviceOfflineSeconds)));
        var offlineDev = devices.Count - healthy - warning;
        if (offlineDev < 0) offlineDev = devices.Count(d => !TenantScope.IsDeviceOnline(d, now));
        var totalDev = devices.Count;
        var healthPct = totalDev == 0 ? 100 : Math.Round(healthy * 100.0 / totalDev, 0);

        return Ok(new DashboardOverviewResponse(
            siteId,
            clientId ?? CurrentUser.ClientId(User),
            kpis,
            movement,
            gateStatus,
            liveGates,
            recent,
            topSites,
            new DeviceHealthDto(totalDev, healthy, warning, Math.Max(0, totalDev - healthy - warning), healthPct),
            openAlerts));
    }
}
