using GateFlow.Api.Authorization;
using GateFlow.Application.Security;
using GateFlow.Contracts.Auth;
using GateFlow.Contracts.Hardware;
using GateFlow.Domain.Entities;
using GateFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Controllers;

[ApiController]
[Route("v1/hardware")]
[Tags("Hardware")]
[Authorize]
public sealed class HardwareController : ControllerBase
{
    private readonly GateFlowDbContext _db;

    public HardwareController(GateFlowDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HardwareDeviceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HardwareDeviceDto>>> List(
        [FromQuery] string? siteId,
        [FromQuery] string? clientId)
    {
        var now = DateTime.UtcNow;
        var rows = await TenantScope.Devices(_db, User, clientId, siteId)
            .Include(d => d.Site)
            .Include(d => d.Gate)
            .OrderBy(d => d.Site!.Name).ThenBy(d => d.Name)
            .ToListAsync();

        // Refresh derived offline status for response
        var dtos = rows.Select(d =>
        {
            var online = TenantScope.IsDeviceOnline(d, now);
            var status = online
                ? (d.ConnectionStatus == HardwareStatuses.Degraded ? HardwareStatuses.Degraded : HardwareStatuses.Online)
                : HardwareStatuses.Offline;
            return new HardwareDeviceDto(
                d.Id, d.ClientId, d.SiteId, d.Site?.Name ?? "—",
                d.GateId, d.Gate?.Name, d.Name, d.DeviceType, d.SerialNumber,
                d.DeviceApiKey, d.FirmwareVersion, status, d.LastSeenAt, d.IsActive);
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost]
    [ProducesResponseType(typeof(HardwareDeviceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<HardwareDeviceDto>> Create([FromBody] CreateHardwareRequest body)
    {
        var role = CurrentUser.Role(User);
        if (role is not (Roles.PlatformAdmin or Roles.ClientAdmin or Roles.SiteManager))
            return Forbid();

        if (string.IsNullOrWhiteSpace(body.SiteId) || string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(new ErrorResponse("INVALID_BODY"));

        var site = await _db.Sites.FirstOrDefaultAsync(s => s.Id == body.SiteId);
        if (site is null || !SiteAccessGuard.CanAccessSite(User, site))
            return Forbid();

        if (!string.IsNullOrWhiteSpace(body.GateId))
        {
            var gateOk = await _db.Lanes.AnyAsync(g => g.Id == body.GateId && g.SiteId == site.Id);
            if (!gateOk) return BadRequest(new ErrorResponse("INVALID_GATE"));
        }

        var device = new HardwareDevice
        {
            ClientId = site.ClientId,
            SiteId = site.Id,
            GateId = body.GateId,
            Name = body.Name.Trim(),
            DeviceType = string.IsNullOrWhiteSpace(body.DeviceType) ? "CONTROLLER" : body.DeviceType.Trim().ToUpperInvariant(),
            SerialNumber = body.SerialNumber,
            DeviceApiKey = $"hw_{Guid.NewGuid():N}",
            ConnectionStatus = HardwareStatuses.Offline,
            RegisteredAt = DateTime.UtcNow,
        };
        _db.HardwareDevices.Add(device);
        await _db.SaveChangesAsync();

        return Created($"/v1/hardware/{device.Id}", new HardwareDeviceDto(
            device.Id, device.ClientId, device.SiteId, site.Name,
            device.GateId, null, device.Name, device.DeviceType, device.SerialNumber,
            device.DeviceApiKey, device.FirmwareVersion, device.ConnectionStatus,
            device.LastSeenAt, device.IsActive));
    }

    /// <summary>Device heartbeat — authenticate with X-Device-Key (hardware key).</summary>
    [HttpPost("heartbeat")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DeviceHeartbeatResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeviceHeartbeatResponse>> Heartbeat(
        [FromHeader(Name = "X-Device-Key")] string? deviceKey,
        [FromBody] DeviceHeartbeatRequest body)
    {
        if (string.IsNullOrWhiteSpace(deviceKey))
            return Unauthorized(new ErrorResponse("DEVICE_KEY_REQUIRED"));

        var device = await _db.HardwareDevices.FirstOrDefaultAsync(d => d.DeviceApiKey == deviceKey);
        if (device is null || !device.IsActive)
            return Unauthorized(new ErrorResponse("UNKNOWN_DEVICE"));

        var status = string.IsNullOrWhiteSpace(body.Status)
            ? HardwareStatuses.Online
            : body.Status.Trim().ToUpperInvariant();

        device.ConnectionStatus = status;
        device.LastSeenAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(body.FirmwareVersion))
            device.FirmwareVersion = body.FirmwareVersion;

        _db.DeviceHeartbeats.Add(new DeviceHeartbeat
        {
            DeviceId = device.Id,
            Status = status,
            FirmwareVersion = body.FirmwareVersion ?? device.FirmwareVersion,
            IpAddress = body.IpAddress,
            SignalRssi = body.SignalRssi,
            Payload = string.IsNullOrWhiteSpace(body.Payload) ? "{}" : body.Payload,
        });

        // Auto-resolve open DEVICE_OFFLINE alerts for this device
        var openAlerts = await _db.Alerts
            .Where(a => a.DeviceId == device.Id && a.Status == AlertStatuses.Open && a.Type == AlertTypes.DeviceOffline)
            .ToListAsync();
        foreach (var a in openAlerts)
        {
            a.Status = AlertStatuses.Resolved;
            a.ResolvedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new DeviceHeartbeatResponse(device.Id, device.ConnectionStatus, device.LastSeenAt!.Value));
    }
}
