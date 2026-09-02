namespace GateFlow.Contracts.Gates;

public sealed record GateListItemDto(
    string Id,
    string SiteId,
    string SiteName,
    string? ClientId,
    string Name,
    string Code,
    string Direction,
    string BarrierState,
    string Status,
    bool IsActive,
    bool DeviceOnline,
    DateTime? LastSeenAt,
    DateTime CreatedAt);

public sealed record CreateGateRequest(
    string SiteId,
    string Name,
    string Code,
    string Direction);

public sealed record GateCommandRequest(
    string Command,
    string? ReasonCode,
    string? ReasonNote,
    string? Method);

public sealed record GateCommandResponse(
    string CommandId,
    string GateId,
    string Command,
    string Status,
    string BarrierState,
    DateTime CreatedAt);
