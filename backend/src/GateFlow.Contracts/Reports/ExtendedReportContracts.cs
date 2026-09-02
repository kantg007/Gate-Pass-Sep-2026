namespace GateFlow.Contracts.Reports;

public sealed record VehicleMovementResponse(
    DateTime From,
    DateTime To,
    IReadOnlyList<VehicleMovementHourDto> Hours);

public sealed record VehicleMovementHourDto(
    string HourLabel,
    DateTime HourStart,
    int Entered,
    int Exited,
    int Inside);

public sealed record TopSitesResponse(
    DateTime From,
    DateTime To,
    IReadOnlyList<TopSiteStatDto> Sites);

public sealed record TopSiteStatDto(
    string SiteId,
    string SiteName,
    int Entries,
    int Exits,
    double? ChangePct);
