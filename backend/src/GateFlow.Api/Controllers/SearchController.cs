using GateFlow.Api.Authorization;
using GateFlow.Contracts.Search;
using GateFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Controllers;

[ApiController]
[Route("v1/search")]
[Tags("Search")]
[Authorize]
public sealed class SearchController : ControllerBase
{
    private readonly GateFlowDbContext _db;

    public SearchController(GateFlowDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResponse>> Search([FromQuery] string q, [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Ok(new SearchResponse(q ?? "", Array.Empty<SearchHitDto>()));

        var term = q.Trim().ToLowerInvariant();
        var take = Math.Clamp(limit, 1, 50);
        var hits = new List<SearchHitDto>();

        var sites = await TenantScope.Sites(_db, User)
            .Where(s => s.Name.ToLower().Contains(term) || s.Slug.ToLower().Contains(term))
            .Take(take)
            .Select(s => new SearchHitDto("site", s.Id, s.Name, s.Slug, $"/sites/{s.Id}"))
            .ToListAsync();
        hits.AddRange(sites);

        var gates = await TenantScope.Gates(_db, User)
            .Where(g => g.Name.ToLower().Contains(term) || g.Code.ToLower().Contains(term))
            .Take(take)
            .Select(g => new SearchHitDto("gate", g.Id, g.Name, g.Code, "/gates"))
            .ToListAsync();
        hits.AddRange(gates);

        var siteIds = await TenantScope.Sites(_db, User).Select(s => s.Id).ToListAsync();
        var vehicles = await _db.Vehicles.AsNoTracking()
            .Where(v => siteIds.Contains(v.SiteId) &&
                        (v.PlateNumber.ToLower().Contains(term) ||
                         (v.Label != null && v.Label.ToLower().Contains(term))))
            .Take(take)
            .Select(v => new SearchHitDto("vehicle", v.Id, v.PlateNumber, v.Label, $"/sites/{v.SiteId}"))
            .ToListAsync();
        hits.AddRange(vehicles);

        var usersQ = _db.Users.AsNoTracking().AsQueryable();
        if (!GateFlow.Application.Security.CurrentUser.IsPlatformAdmin(User))
        {
            var clientId = GateFlow.Application.Security.CurrentUser.ClientId(User);
            usersQ = usersQ.Where(u => u.ClientId == clientId);
        }
        var users = await usersQ
            .Where(u => u.Email.ToLower().Contains(term) || u.FullName.ToLower().Contains(term))
            .Take(take)
            .Select(u => new SearchHitDto("user", u.Id, u.FullName, u.Email, "/users"))
            .ToListAsync();
        hits.AddRange(users);

        return Ok(new SearchResponse(q, hits.Take(take).ToList()));
    }
}
