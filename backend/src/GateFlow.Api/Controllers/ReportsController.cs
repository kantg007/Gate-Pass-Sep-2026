using GateFlow.Application.Security;
using GateFlow.Contracts.Reports;
using GateFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Controllers;

[ApiController]
[Route("v1/reports")]
[Tags("Reports")]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly GateFlowDbContext _db;

    public ReportsController(GateFlowDbContext db) => _db = db;

    [HttpGet("access-summary")]
    [ProducesResponseType(typeof(AccessSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AccessSummaryResponse>> AccessSummary(
        [FromQuery] string? clientId,
        [FromQuery] string? siteId,
        [FromQuery] string? gateId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var fromUtc = from?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(-7);
        var toUtc = to?.ToUniversalTime() ?? DateTime.UtcNow;

        var q = _db.AccessEvents.AsNoTracking()
            .Where(e => e.CreatedAt >= fromUtc && e.CreatedAt <= toUtc);

        if (CurrentUser.IsPlatformAdmin(User))
        {
            if (!string.IsNullOrWhiteSpace(clientId))
                q = q.Where(e => e.ClientId == clientId);
        }
        else
        {
            var scopedClient = CurrentUser.ClientId(User);
            if (scopedClient is null) return Forbid();
            q = q.Where(e => e.ClientId == scopedClient);
            var siteScope = CurrentUser.SiteId(User);
            if (!string.IsNullOrEmpty(siteScope))
                q = q.Where(e => e.SiteId == siteScope);
        }

        if (!string.IsNullOrWhiteSpace(siteId))
            q = q.Where(e => e.SiteId == siteId);
        if (!string.IsNullOrWhiteSpace(gateId))
            q = q.Where(e => e.LaneId == gateId);

        var rows = await q.GroupBy(e => new { e.ClientId, e.SiteId, e.LaneId, e.EventType })
            .Select(g => new AccessSummaryRowDto(
                g.Key.ClientId,
                g.Key.SiteId,
                g.Key.LaneId,
                g.Key.EventType,
                g.Count()))
            .ToListAsync();

        return Ok(new AccessSummaryResponse(fromUtc, toUtc, rows));
    }
}
