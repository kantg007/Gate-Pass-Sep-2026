using GateFlow.Contracts.Health;
using GateFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Controllers;

[ApiController]
[Tags("Health")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    private readonly GateFlowDbContext _db;

    public HealthController(GateFlowDbContext db) => _db = db;

    [HttpGet("/health")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<HealthResponse>> GetHealth() => BuildAsync();

    [HttpGet("/api/health")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<HealthResponse>> GetApiHealth() => BuildAsync();

    private async Task<ActionResult<HealthResponse>> BuildAsync()
    {
        string dbStatus;
        try
        {
            dbStatus = await _db.Database.CanConnectAsync() ? "up" : "down";
        }
        catch
        {
            dbStatus = "down";
        }

        var ok = dbStatus == "up";
        return Ok(new HealthResponse(
            ok ? "Healthy" : "Degraded",
            ok,
            "gateflow-api",
            ".NET",
            DateTime.UtcNow,
            dbStatus));
    }
}
