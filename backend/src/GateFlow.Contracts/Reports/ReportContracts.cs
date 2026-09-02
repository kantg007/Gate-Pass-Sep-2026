namespace GateFlow.Contracts.Reports;

public sealed record AccessSummaryRowDto(
    string? ClientId,
    string SiteId,
    string? GateId,
    string EventType,
    int Count);

public sealed record AccessSummaryResponse(
    DateTime From,
    DateTime To,
    IReadOnlyList<AccessSummaryRowDto> Rows);
