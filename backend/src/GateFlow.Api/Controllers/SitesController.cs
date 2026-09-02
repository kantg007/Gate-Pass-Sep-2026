using System.Text.Json;
using GateFlow.Api.Authorization;
using GateFlow.Api.Mapping;
using GateFlow.Application.Security;
using GateFlow.Contracts.Auth;
using GateFlow.Contracts.Sites;
using GateFlow.Domain.Entities;
using GateFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Controllers;

[ApiController]
[Route("v1/sites")]
[Tags("Sites")]
[Authorize]
public sealed class SitesController : ControllerBase
{
    private readonly GateFlowDbContext _db;

    public SitesController(GateFlowDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SiteListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<SiteListItemDto>>> List()
    {
        var q = _db.Sites.AsQueryable();
        if (!CurrentUser.IsPlatformAdmin(User))
        {
            var clientId = CurrentUser.ClientId(User);
            if (clientId is null) return Forbid();
            q = q.Where(s => s.ClientId == clientId);
            var siteScope = CurrentUser.SiteId(User);
            if (!string.IsNullOrEmpty(siteScope))
                q = q.Where(s => s.Id == siteScope);
        }

        var rows = await q.OrderBy(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.ClientId,
                s.Name,
                s.Slug,
                s.IsActive,
                s.CreatedAt,
                s.UpdatedAt,
                s.Settings,
                Vehicles = s.Vehicles.Count,
                Lanes = s.Lanes.Count,
                Events = s.Events.Count,
            })
            .ToListAsync();

        var sites = rows.Select(s => new SiteListItemDto(
            s.Id,
            s.ClientId,
            s.Name,
            s.Slug,
            s.IsActive,
            s.CreatedAt,
            s.UpdatedAt,
            SiteSettingsMapper.FromJson(s.Settings),
            new SiteCountsDto(s.Vehicles, s.Lanes, s.Events))).ToList();

        return Ok(sites);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateSiteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateSiteResponse>> Create([FromBody] CreateSiteRequest body)
    {
        var role = CurrentUser.Role(User);
        if (role is not (Roles.PlatformAdmin or Roles.ClientAdmin)) return Forbid();

        var clientId = body.ClientId;
        if (role == Roles.ClientAdmin)
            clientId = CurrentUser.ClientId(User);

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(body.Name) ||
            string.IsNullOrWhiteSpace(body.Slug))
        {
            return BadRequest(new ErrorResponse("INVALID_BODY"));
        }

        var site = new Site
        {
            ClientId = clientId!,
            Name = body.Name.Trim(),
            Slug = body.Slug.Trim().ToLowerInvariant(),
            Settings = new SiteSettings().ToJson(),
        };
        _db.Sites.Add(site);
        _db.Lanes.Add(new Lane
        {
            SiteId = site.Id,
            ClientId = clientId,
            Name = "Main Entry",
            Code = "GATE-IN-1",
            Direction = "ENTRY",
            DeviceApiKey = $"dev_{Guid.NewGuid():N}"[..24],
        });
        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created,
            new CreateSiteResponse(site.Id, site.Name, site.Slug, site.ClientId));
    }

    [HttpGet("{siteId}")]
    [ProducesResponseType(typeof(SiteDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SiteDetailDto>> Get(string siteId)
    {
        var site = await _db.Sites.Include(s => s.Lanes).Include(s => s.Units)
            .FirstOrDefaultAsync(s => s.Id == siteId);
        if (site is null) return NotFound(new ErrorResponse("NOT_FOUND"));
        if (!SiteAccessGuard.CanAccessSite(User, site)) return Forbid();

        return Ok(new SiteDetailDto(
            site.Id,
            site.ClientId,
            site.Name,
            site.Slug,
            site.IsActive,
            site.CreatedAt,
            site.UpdatedAt,
            SiteSettingsMapper.FromJson(site.Settings),
            site.Lanes.Select(l => new LaneDetailDto(
                l.Id, l.SiteId, l.ClientId, l.Name, l.Code, l.Direction,
                l.DeviceApiKey, l.SortOrder, l.IsActive, l.Config, l.CreatedAt)).ToList(),
            site.Units.Select(u => new UnitBriefDto(u.Id, u.Label, u.Block, u.Floor)).ToList()));
    }

    [HttpGet("{siteId}/vehicles")]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<VehicleListItemDto>>> ListVehicles(string siteId)
    {
        if (!await SiteAccessGuard.CanAccessSiteIdAsync(User, _db, siteId)) return Forbid();
        var vehicles = await _db.Vehicles.Where(v => v.SiteId == siteId)
            .Include(v => v.Unit).Include(v => v.Credentials)
            .OrderBy(v => v.PlateNumber)
            .Select(v => new VehicleListItemDto(
                v.Id,
                v.PlateNumber,
                v.Label,
                v.IsActive,
                v.Unit == null ? null : new UnitLabelDto(v.Unit.Label),
                v.Credentials.Select(c => new CredentialBriefDto(c.Id, c.Type, c.Code)).ToList()))
            .ToListAsync();
        return Ok(vehicles);
    }

    [HttpPost("{siteId}/vehicles")]
    [ProducesResponseType(typeof(CreateVehicleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateVehicleResponse>> CreateVehicle(
        string siteId,
        [FromBody] CreateVehicleRequest body)
    {
        if (!await SiteAccessGuard.CanAccessSiteIdAsync(User, _db, siteId)) return Forbid();
        if (CurrentUser.Role(User) == Roles.Guard) return Forbid();
        if (string.IsNullOrWhiteSpace(body.PlateNumber))
            return BadRequest(new ErrorResponse("INVALID_BODY"));

        var siteRow = await _db.Sites.AsNoTracking().FirstAsync(s => s.Id == siteId);
        var vehicle = new Vehicle
        {
            SiteId = siteId,
            ClientId = siteRow.ClientId,
            PlateNumber = body.PlateNumber.Trim().ToUpperInvariant(),
            Label = body.Label,
            UnitId = string.IsNullOrWhiteSpace(body.UnitId) ? null : body.UnitId,
        };
        _db.Vehicles.Add(vehicle);
        var creds = new List<AccessCredential>();
        if (!string.IsNullOrWhiteSpace(body.RfidCode))
        {
            var c = new AccessCredential
            {
                SiteId = siteId,
                Type = "RFID",
                Code = body.RfidCode.Trim(),
                VehicleId = vehicle.Id,
            };
            _db.AccessCredentials.Add(c);
            creds.Add(c);
        }
        if (!string.IsNullOrWhiteSpace(body.BarcodeCode))
        {
            var c = new AccessCredential
            {
                SiteId = siteId,
                Type = "BARCODE",
                Code = body.BarcodeCode.Trim(),
                VehicleId = vehicle.Id,
            };
            _db.AccessCredentials.Add(c);
            creds.Add(c);
        }
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new CreateVehicleResponse(
            new VehicleDto(
                vehicle.Id, vehicle.SiteId, vehicle.ClientId, vehicle.UnitId,
                vehicle.PlateNumber, vehicle.Label, vehicle.IsActive, vehicle.IsBlacklisted, vehicle.CreatedAt),
            creds.Select(c => new CredentialBriefDto(c.Id, c.Type, c.Code)).ToList()));
    }

    [HttpPost("{siteId}/visitors")]
    [ProducesResponseType(typeof(CreateVisitorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateVisitorResponse>> CreateVisitor(
        string siteId,
        [FromBody] CreateVisitorRequest body)
    {
        if (!await SiteAccessGuard.CanAccessSiteIdAsync(User, _db, siteId)) return Forbid();
        var site = await _db.Sites.FirstAsync(s => s.Id == siteId);
        if (string.IsNullOrWhiteSpace(body.GuestName))
            return BadRequest(new ErrorResponse("INVALID_BODY"));

        var settings = SiteSettings.Parse(site.Settings);
        var hours = body.ValidHours ?? settings.VisitorDefaultValidHours;
        var maxUses = body.MaxUses ?? settings.VisitorDefaultMaxUses;
        var validUntil = DateTime.UtcNow.AddHours(hours);
        var qrCode = $"VIS-{Guid.NewGuid():N}"[..16].ToUpperInvariant();

        var pass = new VisitorPass
        {
            SiteId = siteId,
            GuestName = body.GuestName.Trim(),
            UnitId = string.IsNullOrWhiteSpace(body.UnitId) ? null : body.UnitId,
            Purpose = body.Purpose,
            MaxUses = maxUses,
            ValidUntil = validUntil,
        };
        _db.VisitorPasses.Add(pass);
        _db.AccessCredentials.Add(new AccessCredential
        {
            SiteId = siteId,
            Type = "QR",
            Code = qrCode,
            VisitorPassId = pass.Id,
            ExpiresAt = validUntil,
        });
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new CreateVisitorResponse(
            new VisitorPassDto(
                pass.Id, pass.SiteId, pass.UnitId, pass.GuestName, pass.Purpose,
                pass.MaxUses, pass.UsedCount, pass.ValidFrom, pass.ValidUntil, pass.IsActive, pass.CreatedAt),
            qrCode));
    }

    [HttpGet("{siteId}/events")]
    [ProducesResponseType(typeof(IReadOnlyList<AccessEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AccessEventDto>>> ListEvents(string siteId, [FromQuery] int? limit)
    {
        if (!await SiteAccessGuard.CanAccessSiteIdAsync(User, _db, siteId)) return Forbid();
        var take = Math.Min(limit ?? 50, 200);
        var events = await _db.AccessEvents.Where(e => e.SiteId == siteId)
            .Include(e => e.Lane).OrderByDescending(e => e.CreatedAt).Take(take).ToListAsync();

        return Ok(events.Select(e => new AccessEventDto(
            e.Id,
            e.Decision,
            e.EventType,
            e.OpenMethod,
            e.Reason,
            e.CredentialType,
            e.CredentialCode,
            e.PlateNumber,
            e.ActorUserId,
            e.CreatedAt,
            e.Lane == null ? null : new LaneBriefDto(e.Lane.Id, e.Lane.Name, e.Lane.Code),
            SafeJson(e.Meta))).ToList());
    }

    [HttpGet("{siteId}/lanes")]
    [ProducesResponseType(typeof(IReadOnlyList<LaneListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<LaneListItemDto>>> ListLanes(string siteId)
    {
        if (!await SiteAccessGuard.CanAccessSiteIdAsync(User, _db, siteId)) return Forbid();
        var lanes = await _db.Lanes.Where(l => l.SiteId == siteId).ToListAsync();
        return Ok(lanes.Select(l => new LaneListItemDto(
            l.Id, l.Name, l.Direction, l.DeviceApiKey, l.IsActive, SafeJson(l.Config))).ToList());
    }

    private static Dictionary<string, object?> SafeJson(string raw)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(raw) ?? new();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }
}
