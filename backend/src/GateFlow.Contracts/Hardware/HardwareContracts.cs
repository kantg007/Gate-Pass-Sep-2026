namespace GateFlow.Contracts.Hardware;

public sealed record HardwareDeviceDto(
    string Id,
    string ClientId,
    string SiteId,
    string SiteName,
    string? GateId,
    string? GateName,
    string Name,
    string DeviceType,
    string? SerialNumber,
    string DeviceApiKey,
    string? FirmwareVersion,
    string ConnectionStatus,
    DateTime? LastSeenAt,
    bool IsActive);

public sealed record CreateHardwareRequest(
    string SiteId,
    string? GateId,
    string Name,
    string DeviceType,
    string? SerialNumber);

public sealed record DeviceHeartbeatRequest(
    string? Status,
    string? FirmwareVersion,
    string? IpAddress,
    int? SignalRssi,
    string? Payload);

public sealed record DeviceHeartbeatResponse(
    string DeviceId,
    string ConnectionStatus,
    DateTime LastSeenAt);
