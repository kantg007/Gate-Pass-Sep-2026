using System.Text.Json.Serialization;

namespace GateFlow.Contracts.Sites;

public sealed class SiteSettingsDto
{
    public bool AllowManualOpen { get; set; } = true;
    public bool AllowRemoteOpen { get; set; } = true;
    public int VisitorDefaultMaxUses { get; set; } = 2;
    public int VisitorDefaultValidHours { get; set; } = 24;
    public bool RequireActiveVehicle { get; set; } = true;
    public bool DenyExpiredCredentials { get; set; } = true;
    public bool DenyBlacklistedVehicles { get; set; } = true;
    public bool LogDeniedAttempts { get; set; } = true;
    public bool AntiPassbackEnabled { get; set; }
    public int HardwareOfflineSeconds { get; set; } = 60;
    public Dictionary<string, bool> Features { get; set; } = new();
}

public sealed record SiteCountsDto(int Vehicles, int Lanes, int Events);

public sealed record SiteListItemDto(
    string Id,
    string ClientId,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    SiteSettingsDto Settings,
    [property: JsonPropertyName("_count")] SiteCountsDto Count);

public sealed record CreateSiteRequest(string Name, string Slug, string? ClientId);
public sealed record CreateSiteResponse(string Id, string Name, string Slug, string ClientId);

public sealed record UnitBriefDto(string Id, string Label, string? Block, string? Floor);

public sealed record LaneDetailDto(
    string Id,
    string SiteId,
    string? ClientId,
    string Name,
    string Code,
    string Direction,
    string DeviceApiKey,
    int SortOrder,
    bool IsActive,
    string Config,
    DateTime CreatedAt);

public sealed record SiteDetailDto(
    string Id,
    string ClientId,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    SiteSettingsDto Settings,
    IReadOnlyList<LaneDetailDto> Lanes,
    IReadOnlyList<UnitBriefDto> Units);

public sealed record UnitLabelDto(string Label);
public sealed record CredentialBriefDto(string Id, string Type, string Code);

public sealed record VehicleListItemDto(
    string Id,
    string PlateNumber,
    string? Label,
    bool IsActive,
    UnitLabelDto? Unit,
    IReadOnlyList<CredentialBriefDto> Credentials);

public sealed record CreateVehicleRequest(
    string PlateNumber,
    string? Label,
    string? UnitId,
    string? RfidCode,
    string? BarcodeCode);

public sealed record VehicleDto(
    string Id,
    string SiteId,
    string? ClientId,
    string? UnitId,
    string PlateNumber,
    string? Label,
    bool IsActive,
    bool IsBlacklisted,
    DateTime CreatedAt);

public sealed record CreateVehicleResponse(VehicleDto Vehicle, IReadOnlyList<CredentialBriefDto> Credentials);

public sealed record CreateVisitorRequest(
    string GuestName,
    string? UnitId,
    string? Purpose,
    int? MaxUses,
    int? ValidHours);

public sealed record VisitorPassDto(
    string Id,
    string SiteId,
    string? UnitId,
    string GuestName,
    string? Purpose,
    int MaxUses,
    int UsedCount,
    DateTime ValidFrom,
    DateTime ValidUntil,
    bool IsActive,
    DateTime CreatedAt);

public sealed record CreateVisitorResponse(VisitorPassDto VisitorPass, string QrPayload);

public sealed record LaneBriefDto(string Id, string Name, string Code);

public sealed record AccessEventDto(
    string Id,
    string Decision,
    string EventType,
    string OpenMethod,
    string Reason,
    string? CredentialType,
    string? CredentialCode,
    string? PlateNumber,
    string? ActorUserId,
    DateTime CreatedAt,
    LaneBriefDto? Lane,
    Dictionary<string, object?>? Meta);

public sealed record LaneListItemDto(
    string Id,
    string Name,
    string Direction,
    string DeviceApiKey,
    bool IsActive,
    Dictionary<string, object?>? Config);
