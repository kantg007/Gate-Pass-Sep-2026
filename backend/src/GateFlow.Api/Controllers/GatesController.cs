using GateFlow.Api.Authorization;
using GateFlow.Application.Security;
using GateFlow.Contracts.Auth;
using GateFlow.Contracts.Gates;
using GateFlow.Domain.Entities;
using GateFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Controllers;

[ApiController]
[Route("v1/gates")]
[Tags("Gates")]
[Authorize]
public sealed class GatesController : ControllerBase
{
    private readonly GateFlowDbContext _db;

    public GatesController(GateFlowDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GateListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GateListItemDto>>> List(
        [FromQuery] string? siteId,
        [FromQuery] string? clientId)
    {
        var now = DateTime.UtcNow;
        var gates = await TenantScope.Gates(_db, User, clientId, siteId)
            .Include(g => g.Site)
            .OrderBy(g => g.Site!.Name).ThenBy(g => g.SortOrder).ThenBy(g => g.Name)
            .ToListAsync();
        var devices = await TenantScope.Devices(_db, User, clientId, siteId).ToListAsync();

        var rows = gates.Select(g =>
        {
            var status = TenantScope.ResolveGateStatus(g, devices, now);
            var lastSeen = devices.Where(d => d.GateId == g.Id).Select(d => d.LastSeenAt).Max();
            return new GateListItemDto(
                g.Id, g.SiteId, g.Site?.Name ?? "—", g.ClientId,
                g.Name, g.Code, g.Direction, g.BarrierState, status,
                g.IsActive, status != "OFFLINE", lastSeen, g.CreatedAt);
        }).ToList();

        return Ok(rows);
    }

    [HttpPost]
    [ProducesResponseType(typeof(GateListItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GateListItemDto>> Create([FromBody] CreateGateRequest body)
    {
        var role = CurrentUser.Role(User);
        if (role is not (Roles.PlatformAdmin or Roles.ClientAdmin or Roles.SiteManager))
            return Forbid();

        if (string.IsNullOrWhiteSpace(body.SiteId) || string.IsNullOrWhiteSpace(body.Name) ||
            string.IsNullOrWhiteSpace(body.Code))
            return BadRequest(new ErrorResponse("INVALID_BODY"));

        var site = await _db.Sites.FirstOrDefaultAsync(s => s.Id == body.SiteId);
        if (site is null || !SiteAccessGuard.CanAccessSite(User, site))
            return Forbid();

        var direction = string.IsNullOrWhiteSpace(body.Direction) ? "ENTRY" : body.Direction.Trim().ToUpperInvariant();
        if (direction is not ("ENTRY" or "EXIT" or "BOTH"))
            return BadRequest(new ErrorResponse("INVALID_DIRECTION"));

        var gate = new Lane
        {
            SiteId = site.Id,
            ClientId = site.ClientId,
            Name = body.Name.Trim(),
            Code = body.Code.Trim().ToUpperInvariant(),
            Direction = direction,
            DeviceApiKey = $"dev_{Guid.NewGuid():N}"[..28],
            BarrierState = BarrierStates.Closed,
            BarrierStateAt = DateTime.UtcNow,
        };
        _db.Lanes.Add(gate);
        await _db.SaveChangesAsync();

        return Created($"/v1/gates/{gate.Id}", new GateListItemDto(
            gate.Id, gate.SiteId, site.Name, gate.ClientId,
            gate.Name, gate.Code, gate.Direction, gate.BarrierState, "CLOSED",
            gate.IsActive, true, null, gate.CreatedAt));
    }

    [HttpPost("{gateId}/commands")]
    [ProducesResponseType(typeof(GateCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GateCommandResponse>> Command(string gateId, [FromBody] GateCommandRequest body)
    {
        var cmd = (body.Command ?? "").Trim().ToUpperInvariant();
        if (cmd is not ("OPEN" or "CLOSE"))
            return BadRequest(new ErrorResponse("INVALID_COMMAND"));

        var gate = await _db.Lanes.Include(g => g.Site).FirstOrDefaultAsync(g => g.Id == gateId);
        if (gate is null || gate.Site is null || !SiteAccessGuard.CanAccessSite(User, gate.Site))
            return Forbid();

        var role = CurrentUser.Role(User);
        var isGuard = role == Roles.Guard;
        if (isGuard)
        {
            var uid = CurrentUser.UserId(User);
            var assigned = await _db.UserGateAssignments.AnyAsync(a => a.UserId == uid && a.GateId == gateId);
            if (!assigned && CurrentUser.SiteId(User) != gate.SiteId)
                return Forbid();
        }

        var method = string.IsNullOrWhiteSpace(body.Method)
            ? (isGuard ? OpenMethods.Guard : OpenMethods.Remote)
            : body.Method.Trim().ToUpperInvariant();

        var reasonCode = string.IsNullOrWhiteSpace(body.ReasonCode) ? "REMOTE" : body.ReasonCode.Trim().ToUpperInvariant();
        var userId = CurrentUser.UserId(User);

        var command = new GateCommand
        {
            GateId = gate.Id,
            SiteId = gate.SiteId,
            ClientId = gate.ClientId,
            Command = cmd,
            Status = "ACKED",
            RequestedByUserId = userId,
            Source = method,
            SentAt = DateTime.UtcNow,
            AckedAt = DateTime.UtcNow,
        };
        _db.GateCommands.Add(command);

        gate.BarrierState = cmd == "OPEN" ? BarrierStates.Open : BarrierStates.Closed;
        gate.BarrierStateAt = DateTime.UtcNow;

        var evt = new AccessEvent
        {
            SiteId = gate.SiteId,
            ClientId = gate.ClientId,
            LaneId = gate.Id,
            Decision = "ALLOW",
            EventType = cmd == "OPEN" ? AccessEventTypes.ManualOpen : AccessEventTypes.ManualClose,
            OpenMethod = method,
            Reason = reasonCode,
            ActorUserId = userId,
            PlateNumber = null,
        };
        _db.AccessEvents.Add(evt);

        _db.ManualOverrides.Add(new ManualOverride
        {
            GateId = gate.Id,
            SiteId = gate.SiteId,
            ClientId = gate.ClientId,
            ActorUserId = userId ?? "",
            Action = cmd,
            Method = method,
            ReasonCode = reasonCode,
            ReasonNote = body.ReasonNote,
            AccessEventId = evt.Id,
            GateCommandId = command.Id,
        });

        await _db.SaveChangesAsync();

        return Ok(new GateCommandResponse(
            command.Id, gate.Id, cmd, command.Status, gate.BarrierState, command.CreatedAt));
    }
}
