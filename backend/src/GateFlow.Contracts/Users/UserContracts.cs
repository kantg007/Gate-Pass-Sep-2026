namespace GateFlow.Contracts.Users;

public sealed record UserListItemDto(
    string Id,
    string Email,
    string FullName,
    string Role,
    string? ClientId,
    string? SiteId,
    string? SiteName,
    string? Phone,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public sealed record CreateUserRequest(
    string Email,
    string FullName,
    string Password,
    string Role,
    string? SiteId,
    string? Phone);

public sealed record UpdateUserRequest(
    string? FullName,
    string? Role,
    string? SiteId,
    string? Phone,
    bool? IsActive);

public sealed record RoleListItemDto(
    string Id,
    string? ClientId,
    string Name,
    string Code,
    string? Description,
    bool IsSystem,
    bool IsActive,
    int PermissionCount,
    int UserCount);

public sealed record PermissionDto(string Id, string Key, string Name, string Module);
