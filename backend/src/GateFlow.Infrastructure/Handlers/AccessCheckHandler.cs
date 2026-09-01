using System.Text.Json;
using GateFlow.Application.Abstractions;
using GateFlow.Contracts.Access;
using GateFlow.Domain.Entities;
using GateFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Infrastructure.Handlers;

public sealed class AccessCheckHandler : IAccessService
{
    private readonly GateFlowDbContext _db;

    public AccessCheckHandler(GateFlowDbContext db) => _db = db;

    public async Task<AccessCheckResponse> CheckAsync(
        AccessCheckRequest input,
        string? deviceApiKey,
        CancellationToken ct = default)
    {
        string? siteId = input.SiteId;
        string? laneId = input.LaneId;

        if (!string.IsNullOrWhiteSpace(deviceApiKey))
        {
            var lane = await _db.Lanes.Include(l => l.Site)
                .FirstOrDefaultAsync(l => l.DeviceApiKey == deviceApiKey, ct);
            if (lane is null || !lane.IsActive || !lane.Site.IsActive)
            {
                return new AccessCheckResponse(false, "DENY", "UNKNOWN_OR_INACTIVE_DEVICE", siteId ?? "", null, null, null, null, "");
            }
            siteId = lane.SiteId;
            laneId = lane.Id;
        }

        if (string.IsNullOrWhiteSpace(siteId))
        {
            return new AccessCheckResponse(false, "DENY", "SITE_REQUIRED", "", null, null, null, null, "");
        }

        var site = await _db.Sites.FirstOrDefaultAsync(s => s.Id == siteId, ct);
        if (site is null || !site.IsActive)
        {
            return await PersistAsync(siteId, laneId, input, "DENY", "SITE_INACTIVE", clientId: site?.ClientId, ct: ct);
        }

        var client = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == site.ClientId, ct);
        if (client is not null && client.Status is "Suspended" or "Pending")
        {
            return await PersistAsync(siteId, laneId, input, "DENY", "CLIENT_SUSPENDED", clientId: site.ClientId, ct: ct);
        }

        var activeSub = await _db.Subscriptions.AsNoTracking()
            .Where(s => s.ClientId == site.ClientId && (s.Status == "Active" || s.Status == "Grace" || s.Status == "Trial"))
            .OrderByDescending(s => s.EndsAt)
            .FirstOrDefaultAsync(ct);
        if (activeSub is not null && activeSub.Status == "Grace" && activeSub.GraceEndsAt is not null && activeSub.GraceEndsAt < DateTime.UtcNow)
        {
            return await PersistAsync(siteId, laneId, input, "DENY", "SUBSCRIPTION_EXPIRED", clientId: site.ClientId, ct: ct);
        }
        if (activeSub is null)
        {
            // No subscription row yet (legacy tenants) — allow; new tenants get seeded/created with one.
        }
        else if (activeSub.Status == "Active" && activeSub.EndsAt < DateTime.UtcNow)
        {
            return await PersistAsync(siteId, laneId, input, "DENY", "SUBSCRIPTION_EXPIRED", clientId: site.ClientId, ct: ct);
        }

        var settings = SiteSettings.Parse(site.Settings);
        var type = input.CredentialType.Trim().ToUpperInvariant();
        var code = input.Code.Trim();

        if (string.IsNullOrEmpty(code))
        {
            return await PersistAsync(siteId, laneId, input, "DENY", "EMPTY_CODE", clientId: site.ClientId, ct: ct);
        }

        if (type == "MANUAL")
        {
            if (!settings.AllowManualOpen)
            {
                return await PersistAsync(siteId, laneId, input, "DENY", "MANUAL_OPEN_DISABLED",
                    clientId: site.ClientId, openMethod: OpenMethods.Guard, ct: ct);
            }
            return await PersistAsync(siteId, laneId, input, "ALLOW", "MANUAL_OPEN",
                clientId: site.ClientId,
                openMethod: OpenMethods.Guard,
                eventType: AccessEventTypes.ManualOpen,
                metaExtra: new Dictionary<string, object?> { ["note"] = code }, ct: ct);
        }

        if (settings.Features.TryGetValue(type.ToLowerInvariant(), out var enabled) && !enabled)
        {
            return await PersistAsync(siteId, laneId, input, "DENY", $"TYPE_DISABLED:{type}", clientId: site.ClientId, ct: ct);
        }

        var credential = await _db.AccessCredentials
            .Include(c => c.Vehicle)
            .Include(c => c.VisitorPass)!.ThenInclude(v => v!.Unit)
            .FirstOrDefaultAsync(c => c.SiteId == siteId && c.Code == code && c.Type == type, ct);

        if (credential is null || !credential.IsActive)
        {
            return await PersistAsync(siteId, laneId, input, "DENY", "UNKNOWN_CREDENTIAL", clientId: site.ClientId, ct: ct);
        }

        if (settings.DenyExpiredCredentials && credential.ExpiresAt is not null && credential.ExpiresAt < DateTime.UtcNow)
        {
            return await PersistAsync(siteId, laneId, input, "DENY", "CREDENTIAL_EXPIRED",
                clientId: site.ClientId,
                vehicleId: credential.VehicleId, plateNumber: credential.Vehicle?.PlateNumber, ct: ct);
        }

        if (credential.VehicleId is not null && credential.Vehicle is not null)
        {
            var v = credential.Vehicle;
            if (settings.RequireActiveVehicle && !v.IsActive)
            {
                return await PersistAsync(siteId, laneId, input, "DENY", "VEHICLE_INACTIVE",
                    clientId: site.ClientId, vehicleId: v.Id, plateNumber: v.PlateNumber, ct: ct);
            }
            if (settings.DenyBlacklistedVehicles && v.IsBlacklisted)
            {
                return await PersistAsync(siteId, laneId, input, "DENY", "VEHICLE_BLACKLISTED",
                    clientId: site.ClientId, vehicleId: v.Id, plateNumber: v.PlateNumber, ct: ct);
            }
            var now = DateTime.UtcNow;
            if (v.ValidFrom is not null && v.ValidFrom > now)
            {
                return await PersistAsync(siteId, laneId, input, "DENY", "VEHICLE_NOT_YET_VALID",
                    clientId: site.ClientId, vehicleId: v.Id, plateNumber: v.PlateNumber, ct: ct);
            }
            if (v.ValidUntil is not null && v.ValidUntil < now)
            {
                return await PersistAsync(siteId, laneId, input, "DENY", "VEHICLE_EXPIRED",
                    clientId: site.ClientId, vehicleId: v.Id, plateNumber: v.PlateNumber, ct: ct);
            }

            return await PersistAsync(siteId, laneId, input, "ALLOW", "RESIDENT_VEHICLE",
                clientId: site.ClientId, vehicleId: v.Id, plateNumber: v.PlateNumber, ct: ct);
        }

        if (credential.VisitorPass is not null)
        {
            var pass = credential.VisitorPass;
            var now = DateTime.UtcNow;
            if (!pass.IsActive)
            {
                return await PersistAsync(siteId, laneId, input, "DENY", "VISITOR_INACTIVE",
                    clientId: site.ClientId, guestName: pass.GuestName, ct: ct);
            }
            if (pass.ValidFrom > now || pass.ValidUntil < now)
            {
                return await PersistAsync(siteId, laneId, input, "DENY", "VISITOR_OUTSIDE_WINDOW",
                    clientId: site.ClientId, guestName: pass.GuestName, ct: ct);
            }
            if (pass.UsedCount >= pass.MaxUses)
            {
                return await PersistAsync(siteId, laneId, input, "DENY", "VISITOR_MAX_USES",
                    clientId: site.ClientId, guestName: pass.GuestName, ct: ct);
            }

            pass.UsedCount += 1;
            await _db.SaveChangesAsync(ct);

            return await PersistAsync(siteId, laneId, input, "ALLOW", "VISITOR_PASS",
                clientId: site.ClientId,
                guestName: pass.GuestName,
                metaExtra: new Dictionary<string, object?>
                {
                    ["unitLabel"] = pass.Unit?.Label,
                    ["usesLeft"] = pass.MaxUses - pass.UsedCount,
                },
                ct: ct);
        }

        return await PersistAsync(siteId, laneId, input, "DENY", "CREDENTIAL_NOT_LINKED", clientId: site.ClientId, ct: ct);
    }

    private async Task<AccessCheckResponse> PersistAsync(
        string siteId,
        string? laneId,
        AccessCheckRequest input,
        string decision,
        string reason,
        string? clientId = null,
        string? vehicleId = null,
        string? plateNumber = null,
        string? guestName = null,
        string? openMethod = null,
        string? eventType = null,
        Dictionary<string, object?>? metaExtra = null,
        CancellationToken ct = default)
    {
        var site = await _db.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == siteId, ct);
        clientId ??= site?.ClientId;
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

            var resolvedEventType = eventType
                ?? (decision == "ALLOW" ? AccessEventTypes.Pass : AccessEventTypes.Fail);

            var evt = new AccessEvent
            {
                SiteId = siteId,
                ClientId = clientId,
                LaneId = laneId,
                CredentialType = input.CredentialType.ToUpperInvariant(),
                CredentialCode = input.Code,
                Decision = decision,
                EventType = resolvedEventType,
                OpenMethod = openMethod ?? OpenMethods.Auto,
                Reason = reason,
                VehicleId = vehicleId,
                PlateNumber = plateNumber,
                Meta = JsonSerializer.Serialize(meta),
            };
            _db.AccessEvents.Add(evt);
            await _db.SaveChangesAsync(ct);
            eventId = evt.Id;
        }

        return new AccessCheckResponse(
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
