using GateFlow.Application.Security;
using GateFlow.Contracts.Reports;
using GateFlow.Domain.Entities;
using GateFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Controllers;

[ApiController]
[Route("v1/reports")]
[Tags("Reports")]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly GateFlowDbContext _db;

    public ReportsController(GateFlowDbContext db) => _db = db;

    [HttpGet("access-summary")]
    [ProducesResponseType(typeof(AccessSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AccessSummaryResponse>> AccessSummary(
        [FromQuery] string? clientId,
        [FromQuery] string? siteId,
        [FromQuery] string? gateId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var fromUtc = from?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(-7);
        var toUtc = to?.ToUniversalTime() ?? DateTime.UtcNow;

        var q = _db.AccessEvents.AsNoTracking()
            .Where(e => e.CreatedAt >= fromUtc && e.CreatedAt <= toUtc);

        if (CurrentUser.IsPlatformAdmin(User))
        {
            if (!string.IsNullOrWhiteSpace(clientId))
                q = q.Where(e => e.ClientId == clientId);
        }
        else
        {
            var scopedClient = CurrentUser.ClientId(User);
            if (scopedClient is null) return Forbid();
            q = q.Where(e => e.ClientId == scopedClient);
            var siteScope = CurrentUser.SiteId(User);
            if (!string.IsNullOrEmpty(siteScope))
                q = q.Where(e => e.SiteId == siteScope);
        }

        if (!string.IsNullOrWhiteSpace(siteId))
            q = q.Where(e => e.SiteId == siteId);
        if (!string.IsNullOrWhiteSpace(gateId))
            q = q.Where(e => e.LaneId == gateId);

        var rows = await q.GroupBy(e => new { e.ClientId, e.SiteId, e.LaneId, e.EventType })
            .Select(g => new AccessSummaryRowDto(
                g.Key.ClientId,
                g.Key.SiteId,
                g.Key.LaneId,
                g.Key.EventType,
                g.Count()))
            .ToListAsync();

        return Ok(new AccessSummaryResponse(fromUtc, toUtc, rows));
    }

    [HttpGet("vehicle-movement")]
    [ProducesResponseType(typeof(VehicleMovementResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleMovementResponse>> VehicleMovement(
        [FromQuery] string? clientId,
        [FromQuery] string? siteId,
        [FromQuery] int hours = 24)
    {
        hours = Math.Clamp(hours, 1, 168);
        var toUtc = DateTime.UtcNow;
        var fromUtc = toUtc.AddHours(-(hours - 1));
        fromUtc = new DateTime(fromUtc.Year, fromUtc.Month, fromUtc.Day, fromUtc.Hour, 0, 0, DateTimeKind.Utc);

        var gates = await Authorization.TenantScope.Gates(_db, User, clientId, siteId).ToListAsync();
        var gateDir = gates.ToDictionary(g => g.Id, g => g.Direction);

        var events = await Authorization.TenantScope.Events(_db, User, clientId, siteId)
            .Where(e => e.CreatedAt >= fromUtc && e.EventType == AccessEventTypes.Pass)
            .Select(e => new { e.CreatedAt, e.LaneId })
            .ToListAsync();

        var points = new List<VehicleMovementHourDto>();
        var inside = 0;
        for (var i = 0; i < hours; i++)
        {
            var hourStart = fromUtc.AddHours(i);
            var hourEnd = hourStart.AddHours(1);
            var slice = events.Where(e => e.CreatedAt >= hourStart && e.CreatedAt < hourEnd).ToList();
            var entered = slice.Count(e =>
                e.LaneId is not null && gateDir.TryGetValue(e.LaneId, out var d) && d is "ENTRY" or "BOTH");
            var exited = slice.Count(e =>
                e.LaneId is not null && gateDir.TryGetValue(e.LaneId, out var d) && d == "EXIT");
            entered += slice.Count(e => e.LaneId is null || !gateDir.ContainsKey(e.LaneId!));
            inside = Math.Max(0, inside + entered - exited);
            points.Add(new VehicleMovementHourDto(hourStart.ToString("HH:mm"), hourStart, entered, exited, inside));
        }

        return Ok(new VehicleMovementResponse(fromUtc, toUtc, points));
    }

    [HttpGet("top-sites")]
    [ProducesResponseType(typeof(TopSitesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TopSitesResponse>> TopSites(
        [FromQuery] string? clientId,
        [FromQuery] int limit = 5)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var yesterday = today.AddDays(-1);
        limit = Math.Clamp(limit, 1, 50);

        var siteNames = await Authorization.TenantScope.Sites(_db, User, clientId)
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var todayCounts = await Authorization.TenantScope.Events(_db, User, clientId)
            .Where(e => e.CreatedAt >= today && e.EventType == AccessEventTypes.Pass)
            .GroupBy(e => e.SiteId)
            .Select(g => new { SiteId = g.Key, Entries = g.Count() })
            .ToListAsync();

        var yest = await Authorization.TenantScope.Events(_db, User, clientId)
            .Where(e => e.CreatedAt >= yesterday && e.CreatedAt < today && e.EventType == AccessEventTypes.Pass)
            .GroupBy(e => e.SiteId)
            .Select(g => new { SiteId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SiteId, x => x.Count);

        var exits = await Authorization.TenantScope.Events(_db, User, clientId)
            .Where(e => e.CreatedAt >= today && e.EventType == AccessEventTypes.Pass)
            .Join(_db.Lanes.AsNoTracking().Where(l => l.Direction == "EXIT"),
                e => e.LaneId, l => l.Id, (e, _) => e)
            .GroupBy(e => e.SiteId)
            .Select(g => new { SiteId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SiteId, x => x.Count);

        var rows = todayCounts
            .OrderByDescending(x => x.Entries)
            .Take(limit)
            .Select(x => new TopSiteStatDto(
                x.SiteId,
                siteNames.GetValueOrDefault(x.SiteId, "—"),
                x.Entries,
                exits.GetValueOrDefault(x.SiteId, 0),
                Authorization.TenantScope.PctChange(x.Entries, yest.GetValueOrDefault(x.SiteId, 0))))
            .ToList();

        if (rows.Count == 0)
        {
            rows = siteNames.Take(limit)
                .Select(kv => new TopSiteStatDto(kv.Key, kv.Value, 0, 0, 0))
                .ToList();
        }

        return Ok(new TopSitesResponse(today, now, rows));
    }
}
