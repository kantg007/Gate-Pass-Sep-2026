namespace GateFlow.Contracts.Auth;

public sealed record RegisterRequest(string CompanyName, string FullName, string Email, string Password, string? Phone);
public sealed record LoginRequest(string Email, string Password);
public sealed record ClientSummaryDto(string Id, string Name, string Status);
public sealed record AuthUserDto(
    string Id, string Email, string FullName, string Role,
    string? ClientId, string? SiteId, ClientSummaryDto? Client);
public sealed record AuthResponse(string Token, AuthUserDto User);
public sealed record ErrorResponse(string Error);
