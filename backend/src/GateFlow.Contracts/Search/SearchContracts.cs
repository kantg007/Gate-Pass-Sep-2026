namespace GateFlow.Contracts.Search;

public sealed record SearchHitDto(
    string Type,
    string Id,
    string Title,
    string? Subtitle,
    string? Href);

public sealed record SearchResponse(
    string Query,
    IReadOnlyList<SearchHitDto> Hits);
