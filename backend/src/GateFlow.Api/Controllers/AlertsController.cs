using GateFlow.Api.Authorization;
using GateFlow.Application.Security;
using GateFlow.Contracts.Alerts;
using GateFlow.Contracts.Auth;
using GateFlow.Domain.Entities;
using GateFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Controllers;

[ApiController]
[Route("v1/alerts")]
[Tags("Alerts")]
[Authorize]
public sealed class AlertsController : ControllerBase
{
    private readonly GateFlowDbContext _db;

    public AlertsController(GateFlowDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(AlertListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AlertListResponse>> List(
        [FromQuery] string? siteId,
        [FromQuery] string? clientId,
        [FromQuery] string? status,
        [FromQuery] int limit = 50)
    {
        await EnsureDerivedAlertsAsync(siteId, clientId);

        var q = TenantScope.Alerts(_db, User, clientId, siteId);
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(a => a.Status == status.Trim().ToUpperInvariant());

        var openCount = await TenantScope.Alerts(_db, User, clientId, siteId)
            .CountAsync(a => a.Status == AlertStatuses.Open);

        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(a => new AlertDto(
                a.Id, a.ClientId, a.SiteId,
                a.Site != null ? a.Site.Name : null,
                a.GateId, a.DeviceId,
                a.Severity, a.Type, a.Title, a.Message, a.Status,
                a.CreatedAt, a.AcknowledgedAt, a.ResolvedAt))
            .ToListAsync();

        return Ok(new AlertListResponse(items, openCount));
    }

    [HttpPatch("{alertId}")]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AlertDto>> UpdateStatus(string alertId, [FromBody] UpdateAlertStatusRequest body)
    {
        var alert = await _db.Alerts.Include(a => a.Site).FirstOrDefaultAsync(a => a.Id == alertId);
        if (alert is null) return NotFound();

        if (!CurrentUser.IsPlatformAdmin(User))
        {
            var clientId = CurrentUser.ClientId(User);
            if (clientId is null || alert.ClientId != clientId) return Forbid();
        }

        var next = (body.Status ?? "").Trim().ToUpperInvariant();
        if (next is not (AlertStatuses.Open or AlertStatuses.Acknowledged or AlertStatuses.Resolved))
            return BadRequest(new ErrorResponse("INVALID_STATUS"));

        alert.Status = next;
        if (next == AlertStatuses.Acknowledged)
        {
            alert.AcknowledgedAt = DateTime.UtcNow;
            alert.AcknowledgedByUserId = CurrentUser.UserId(User);
        }
        if (next == AlertStatuses.Resolved)
            alert.ResolvedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new AlertDto(
            alert.Id, alert.ClientId, alert.SiteId, alert.Site?.Name,
            alert.GateId, alert.DeviceId, alert.Severity, alert.Type,
            alert.Title, alert.Message, alert.Status,
            alert.CreatedAt, alert.AcknowledgedAt, alert.ResolvedAt));
    }

    private async Task EnsureDerivedAlertsAsync(string? siteId, string? clientId)
    {
        var now = DateTime.UtcNow;
        var devices = await TenantScope.Devices(_db, User, clientId, siteId).ToListAsync();
        foreach (var d in devices.Where(d => !TenantScope.IsDeviceOnline(d, now)))
        {
            var exists = await _db.Alerts.AnyAsync(a =>
                a.DeviceId == d.Id && a.Type == AlertTypes.DeviceOffline && a.Status == AlertStatuses.Open);
            if (exists) continue;
            _db.Alerts.Add(new Alert
            {
                ClientId = d.ClientId,
                SiteId = d.SiteId,
                GateId = d.GateId,
                DeviceId = d.Id,
                Severity = AlertSeverities.Critical,
                Type = AlertTypes.DeviceOffline,
                Title = "Device offline",
                Message = $"{d.Name} has not sent a heartbeat in {TenantScope.DeviceOfflineSeconds / 60} minutes.",
            });
        }

        // Recent deny spike: >5 fails in last hour at a site
        var hourAgo = now.AddHours(-1);
        var failGroups = await TenantScope.Events(_db, User, clientId, siteId)
            .Where(e => e.CreatedAt >= hourAgo && e.EventType == AccessEventTypes.Fail)
            .GroupBy(e => new { e.SiteId, e.ClientId })
            .Select(g => new { g.Key.SiteId, g.Key.ClientId, Count = g.Count() })
            .Where(x => x.Count >= 5)
            .ToListAsync();

        foreach (var g in failGroups)
        {
            var exists = await _db.Alerts.AnyAsync(a =>
                a.SiteId == g.SiteId && a.Type == AlertTypes.AccessDenied
                && a.Status == AlertStatuses.Open && a.CreatedAt >= hourAgo);
            if (exists) continue;
            _db.Alerts.Add(new Alert
            {
                ClientId = g.ClientId,
                SiteId = g.SiteId,
                Severity = AlertSeverities.Warning,
                Type = AlertTypes.AccessDenied,
                Title = "Access denied spike",
                Message = $"{g.Count} denied attempts in the last hour.",
            });
        }

        await _db.SaveChangesAsync();
    }
}
