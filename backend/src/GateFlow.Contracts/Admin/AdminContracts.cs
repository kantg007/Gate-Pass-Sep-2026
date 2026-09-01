namespace GateFlow.Contracts.Admin;

public sealed record ClientListItemDto(
    string Id,
    string Name,
    string? ContactEmail,
    string? Phone,
    string Status,
    DateTime CreatedAt,
    int SiteCount,
    int UserCount);

public sealed record UpdateClientStatusRequest(string Status);
public sealed record ClientStatusResponse(string Id, string Status);
