using GateFlow.Api.Authorization;
using GateFlow.Application.Security;
using GateFlow.Contracts.Auth;
using GateFlow.Contracts.Users;
using GateFlow.Domain.Entities;
using GateFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Controllers;

[ApiController]
[Route("v1/users")]
[Tags("Users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly GateFlowDbContext _db;

    public UsersController(GateFlowDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> List([FromQuery] string? siteId)
    {
        var role = CurrentUser.Role(User);
        if (role is not (Roles.PlatformAdmin or Roles.ClientAdmin or Roles.SiteManager))
            return Forbid();

        var q = _db.Users.AsNoTracking().AsQueryable();
        if (CurrentUser.IsPlatformAdmin(User))
        {
            // platform sees all
        }
        else
        {
            var clientId = CurrentUser.ClientId(User);
            if (clientId is null) return Forbid();
            q = q.Where(u => u.ClientId == clientId);
        }

        if (!string.IsNullOrWhiteSpace(siteId))
            q = q.Where(u => u.SiteId == siteId);

        var rows = await q.OrderBy(u => u.FullName)
            .Select(u => new UserListItemDto(
                u.Id, u.Email, u.FullName, u.Role, u.ClientId, u.SiteId,
                u.Site != null ? u.Site.Name : null,
                u.Phone, u.IsActive, u.LastLoginAt, u.CreatedAt))
            .ToListAsync();

        return Ok(rows);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserListItemDto>> Create([FromBody] CreateUserRequest body)
    {
        var role = CurrentUser.Role(User);
        if (role is not (Roles.PlatformAdmin or Roles.ClientAdmin))
            return Forbid();

        if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password) ||
            string.IsNullOrWhiteSpace(body.FullName) || string.IsNullOrWhiteSpace(body.Role))
            return BadRequest(new ErrorResponse("INVALID_BODY"));

        var newRole = body.Role.Trim();
        if (newRole is not (Roles.ClientAdmin or Roles.Guard or Roles.SiteManager or Roles.Viewer))
            return BadRequest(new ErrorResponse("INVALID_ROLE"));

        var clientId = CurrentUser.IsPlatformAdmin(User)
            ? CurrentUser.ClientId(User) // may be null — require site for client binding
            : CurrentUser.ClientId(User);

        if (clientId is null)
        {
            // Platform creating user needs an existing client context via site
            if (string.IsNullOrWhiteSpace(body.SiteId))
                return BadRequest(new ErrorResponse("SITE_REQUIRED"));
            var site = await _db.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == body.SiteId);
            if (site is null) return BadRequest(new ErrorResponse("INVALID_SITE"));
            clientId = site.ClientId;
        }
        else if (!string.IsNullOrWhiteSpace(body.SiteId))
        {
            var ok = await SiteAccessGuard.CanAccessSiteIdAsync(User, _db, body.SiteId);
            if (!ok) return Forbid();
        }

        if (await _db.Users.AnyAsync(u => u.Email == body.Email.Trim().ToLowerInvariant()))
            return BadRequest(new ErrorResponse("EMAIL_EXISTS"));

        var user = new AppUser
        {
            Email = body.Email.Trim().ToLowerInvariant(),
            FullName = body.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.Password),
            Role = newRole,
            ClientId = clientId,
            SiteId = body.SiteId,
            Phone = body.Phone,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        string? siteName = null;
        if (user.SiteId is not null)
            siteName = await _db.Sites.Where(s => s.Id == user.SiteId).Select(s => s.Name).FirstOrDefaultAsync();

        return Created($"/v1/users/{user.Id}", new UserListItemDto(
            user.Id, user.Email, user.FullName, user.Role, user.ClientId, user.SiteId,
            siteName, user.Phone, user.IsActive, user.LastLoginAt, user.CreatedAt));
    }

    [HttpPatch("{userId}")]
    public async Task<ActionResult<UserListItemDto>> Update(string userId, [FromBody] UpdateUserRequest body)
    {
        var role = CurrentUser.Role(User);
        if (role is not (Roles.PlatformAdmin or Roles.ClientAdmin))
            return Forbid();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return NotFound();

        if (!CurrentUser.IsPlatformAdmin(User) && user.ClientId != CurrentUser.ClientId(User))
            return Forbid();

        if (!string.IsNullOrWhiteSpace(body.FullName)) user.FullName = body.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(body.Role)) user.Role = body.Role.Trim();
        if (body.SiteId is not null) user.SiteId = string.IsNullOrWhiteSpace(body.SiteId) ? null : body.SiteId;
        if (body.Phone is not null) user.Phone = body.Phone;
        if (body.IsActive is not null) user.IsActive = body.IsActive.Value;

        await _db.SaveChangesAsync();

        string? siteName = null;
        if (user.SiteId is not null)
            siteName = await _db.Sites.Where(s => s.Id == user.SiteId).Select(s => s.Name).FirstOrDefaultAsync();

        return Ok(new UserListItemDto(
            user.Id, user.Email, user.FullName, user.Role, user.ClientId, user.SiteId,
            siteName, user.Phone, user.IsActive, user.LastLoginAt, user.CreatedAt));
    }

    [HttpGet("~/v1/roles")]
    [ProducesResponseType(typeof(IReadOnlyList<RoleListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleListItemDto>>> ListRoles()
    {
        var q = _db.Roles.AsNoTracking().AsQueryable();
        if (!CurrentUser.IsPlatformAdmin(User))
        {
            var clientId = CurrentUser.ClientId(User);
            if (clientId is null) return Forbid();
            q = q.Where(r => r.ClientId == clientId || r.ClientId == null);
        }

        var rows = await q.OrderBy(r => r.Name)
            .Select(r => new RoleListItemDto(
                r.Id, r.ClientId, r.Name, r.Code, r.Description, r.IsSystem, r.IsActive,
                r.RolePermissions.Count, r.UserRoles.Count))
            .ToListAsync();
        return Ok(rows);
    }

    [HttpGet("~/v1/permissions")]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> ListPermissions()
    {
        var rows = await _db.Permissions.AsNoTracking()
            .OrderBy(p => p.Module).ThenBy(p => p.Key)
            .Select(p => new PermissionDto(p.Id, p.Key, p.Name, p.Module))
            .ToListAsync();
        return Ok(rows);
    }
}
