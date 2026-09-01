using System.Text.Json;
using GateFlow.Api.Data;
using GateFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Services;

public record AccessCheckRequest(
    string CredentialType,
    string Code,
    string? SiteId = null,
    string? LaneId = null,
    string? DeviceApiKey = null,
    Dictionary<string, object?>? Meta = null);

public record AccessCheckResult(
    bool Open,
    string Decision,
    string Reason,
    string SiteId,
    string? LaneId,
    string? PlateNumber,
    string? VehicleId,
    string? GuestName,
    string EventId);

public class AccessService
{
    private readonly GateFlowDbContext _db;

    public AccessService(GateFlowDbContext db) => _db = db;

    public async Task<AccessCheckResult> CheckAsync(AccessCheckRequest input, CancellationToken ct = default)
    {
        string? siteId = input.SiteId;
        string? laneId = input.LaneId;

        if (!string.IsNullOrWhiteSpace(input.DeviceApiKey))
        {
            var lane = await _db.Lanes.Include(l => l.Site)
                .FirstOrDefaultAsync(l => l.DeviceApiKey == input.DeviceApiKey, ct);
            if (lane is null || !lane.IsActive || !lane.Site.IsActive)
            {
                return new AccessCheckResult(false, "DENY", "UNKNOWN_OR_INACTIVE_DEVICE", siteId ?? "", null, null, null, null, "");
            }
            siteId = lane.SiteId;
            laneId = lane.Id;
        }

        if (string.IsNullOrWhiteSpace(siteId))
        {
            return new AccessCheckResult(false, "DENY", "SITE_REQUIRED", "", null, null, null, null, "");
        }

        var site = await _db.Sites.FirstOrDefaultAsync(s => s.Id == siteId, ct);
        if (site is null || !site.IsActive)
        {
            return await PersistAsync(siteId, laneId, input, "DENY", "SITE_INACTIVE", ct: ct);
        }

        var settings = SiteSettings.Parse(site.Settings);
        var type = input.CredentialType.Trim().ToUpperInvariant();
        var code = input.Code.Trim();

        if (string.IsNullOrEmpty(code))
        {
            return await PersistAsync(siteId, laneId, input, "DENY", "EMPTY_CODE", ct: ct);
        }

        if (type == "MANUAL")
        {
            if (!settings.AllowManualOpen)
            {
                return await PersistAsync(siteId, laneId, input, "DENY", "MANUAL_OPEN_DISABLED", ct: ct);
            }
            return await PersistAsync(siteId, laneId, input, "ALLOW", "MANUAL_OPEN",
                metaExtra: new Dictionary<string, object?> { ["note"] = code }, ct: ct);
        }

        if (settings.Features.TryGetValue(type.ToLowerInvariant(), out var enabled) && !enabled)
        {
            return await PersistAsync(siteId, laneId, input, "DENY", $"TYPE_DISABLED:{type}", ct: ct);
        }

        var credential = await _db.AccessCredentials
            .Include(c => c.Vehicle)
            .Include(c => c.VisitorPass)!.ThenInclude(v => v!.Unit)
            .FirstOrDefaultAsync(c => c.SiteId == siteId && c.Code == code && c.Type == type, ct);

        if (credential is null || !credential.IsActive)
        {
            return await PersistAsync(siteId, laneId, input, "DENY", "UNKNOWN_CREDENTIAL", ct: ct);
        }

        if (settings.DenyExpiredCredentials && credential.ExpiresAt is not null && credential.ExpiresAt < DateTime.UtcNow)
        {
            return await PersistAsync(siteId, laneId, input, "DENY", "CREDENTIAL_EXPIRED",
                vehicleId: credential.VehicleId, plateNumber: credential.Vehicle?.PlateNumber, ct: ct);
        }

        if (credential.VehicleId is not null && credential.Vehicle is not null)
        {
            if (settings.RequireActiveVehicle && !credential.Vehicle.IsActive)
            {
                return await PersistAsync(siteId, laneId, input, "DENY", "VEHICLE_INACTIVE",
                    vehicleId: credential.VehicleId, plateNumber: credential.Vehicle.PlateNumber, ct: ct);
            }

            return await PersistAsync(siteId, laneId, input, "ALLOW", "RESIDENT_VEHICLE",
                vehicleId: credential.VehicleId, plateNumber: credential.Vehicle.PlateNumber, ct: ct);
        }

        if (credential.VisitorPass is not null)
        {
            var pass = credential.VisitorPass;
            var now = DateTime.UtcNow;
            if (!pass.IsActive)
            {
                return await PersistAsync(siteId, laneId, input, "DENY", "VISITOR_INACTIVE", guestName: pass.GuestName, ct: ct);
            }
            if (pass.ValidFrom > now || pass.ValidUntil < now)
            {
                return await PersistAsync(siteId, laneId, input, "DENY", "VISITOR_OUTSIDE_WINDOW", guestName: pass.GuestName, ct: ct);
            }
            if (pass.UsedCount >= pass.MaxUses)
            {
                return await PersistAsync(siteId, laneId, input, "DENY", "VISITOR_MAX_USES", guestName: pass.GuestName, ct: ct);
            }

            pass.UsedCount += 1;
            await _db.SaveChangesAsync(ct);

            return await PersistAsync(siteId, laneId, input, "ALLOW", "VISITOR_PASS",
                guestName: pass.GuestName,
                metaExtra: new Dictionary<string, object?>
                {
                    ["unitLabel"] = pass.Unit?.Label,
                    ["usesLeft"] = pass.MaxUses - pass.UsedCount,
                },
                ct: ct);
        }

        return await PersistAsync(siteId, laneId, input, "DENY", "CREDENTIAL_NOT_LINKED", ct: ct);
    }

    private async Task<AccessCheckResult> PersistAsync(
        string siteId,
        string? laneId,
        AccessCheckRequest input,
        string decision,
        string reason,
        string? vehicleId = null,
        string? plateNumber = null,
        string? guestName = null,
        Dictionary<string, object?>? metaExtra = null,
        CancellationToken ct = default)
    {
        var site = await _db.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == siteId, ct);
        var settings = SiteSettings.Parse(site?.Settings);
        var shouldLog = decision == "ALLOW" || settings.LogDeniedAttempts;
        var eventId = "";

        if (shouldLog && !string.IsNullOrEmpty(siteId))
        {
            var meta = new Dictionary<string, object?>();
            if (input.Meta is not null)
            {
                foreach (var kv in input.Meta) meta[kv.Key] = kv.Value;
            }
            if (metaExtra is not null)
            {
                foreach (var kv in metaExtra) meta[kv.Key] = kv.Value;
            }
            if (guestName is not null) meta["guestName"] = guestName;

            var evt = new AccessEvent
            {
                SiteId = siteId,
                LaneId = laneId,
                CredentialType = input.CredentialType.ToUpperInvariant(),
                CredentialCode = input.Code,
                Decision = decision,
                Reason = reason,
                VehicleId = vehicleId,
                PlateNumber = plateNumber,
                Meta = JsonSerializer.Serialize(meta),
            };
            _db.AccessEvents.Add(evt);
            await _db.SaveChangesAsync(ct);
            eventId = evt.Id;
        }

        return new AccessCheckResult(
            decision == "ALLOW",
            decision,
            reason,
            siteId,
            laneId,
            plateNumber,
            vehicleId,
            guestName,
            eventId);
    }
}
