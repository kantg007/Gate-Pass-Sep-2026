namespace GateFlow.Contracts.Alerts;

public sealed record AlertDto(
    string Id,
    string? ClientId,
    string? SiteId,
    string? SiteName,
    string? GateId,
    string? DeviceId,
    string Severity,
    string Type,
    string Title,
    string Message,
    string Status,
    DateTime CreatedAt,
    DateTime? AcknowledgedAt,
    DateTime? ResolvedAt);

public sealed record UpdateAlertStatusRequest(string Status);

public sealed record AlertListResponse(
    IReadOnlyList<AlertDto> Items,
    int OpenCount);
