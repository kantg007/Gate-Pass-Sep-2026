namespace GateFlow.Contracts.Dashboard;

public sealed record KpiCardDto(
    string Key,
    string Label,
    double Value,
    double? ChangePct,
    string CompareLabel);

public sealed record VehicleMovementPointDto(
    string HourLabel,
    DateTime HourStart,
    int Entered,
    int Exited,
    int Inside);

public sealed record GateStatusSummaryDto(int Open, int Closed, int Offline, int Total);

public sealed record LiveGateRowDto(
    string Id,
    string Name,
    string Code,
    string SiteId,
    string SiteName,
    string Direction,
    /// <summary>OPEN | CLOSED | OFFLINE</summary>
    string Status,
    string BarrierState,
    bool DeviceOnline,
    DateTime? LastSeenAt);

public sealed record RecentActivityDto(
    string Id,
    string Kind,
    string Title,
    string? Detail,
    string? PlateNumber,
    string? SiteName,
    string? GateName,
    string Decision,
    string EventType,
    DateTime CreatedAt);

public sealed record TopSiteRowDto(
    string SiteId,
    string SiteName,
    int Entries,
    double? ChangePct);

public sealed record DeviceHealthDto(
    int Total,
    int Healthy,
    int Warning,
    int Offline,
    double HealthyPct);

public sealed record DashboardOverviewResponse(
    string? SiteId,
    string? ClientId,
    IReadOnlyList<KpiCardDto> Kpis,
    IReadOnlyList<VehicleMovementPointDto> VehicleMovement,
    GateStatusSummaryDto GateStatus,
    IReadOnlyList<LiveGateRowDto> LiveGates,
    IReadOnlyList<RecentActivityDto> RecentActivity,
    IReadOnlyList<TopSiteRowDto> TopSites,
    DeviceHealthDto DeviceHealth,
    int OpenAlerts);
