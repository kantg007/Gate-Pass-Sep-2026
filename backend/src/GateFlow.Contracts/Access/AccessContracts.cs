namespace GateFlow.Contracts.Access;

public sealed record AccessCheckRequest(
    string CredentialType,
    string Code,
    string? SiteId = null,
    string? LaneId = null,
    Dictionary<string, object?>? Meta = null);

public sealed record AccessCheckResponse(
    bool Open,
    string Decision,
    string Reason,
    string SiteId,
    string? LaneId,
    string? PlateNumber,
    string? VehicleId,
    string? GuestName,
    string EventId);
