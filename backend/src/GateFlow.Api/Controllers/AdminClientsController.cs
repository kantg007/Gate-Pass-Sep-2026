using GateFlow.Application.Security;
using GateFlow.Contracts.Admin;
using GateFlow.Contracts.Auth;
using GateFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Controllers;

[ApiController]
[Route("v1/admin/clients")]
[Tags("Admin")]
[Authorize]
public sealed class AdminClientsController : ControllerBase
{
    private readonly GateFlowDbContext _db;

    public AdminClientsController(GateFlowDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClientListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ClientListItemDto>>> List()
    {
        if (!CurrentUser.IsPlatformAdmin(User)) return Forbid();
        var clients = await _db.Clients
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ClientListItemDto(
                c.Id,
                c.Name,
                c.ContactEmail,
                c.Phone,
                c.Status,
                c.CreatedAt,
                c.Sites.Count,
                c.Users.Count))
            .ToListAsync();
        return Ok(clients);
    }

    [HttpPatch("{clientId}/status")]
    [ProducesResponseType(typeof(ClientStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ClientStatusResponse>> UpdateStatus(
        string clientId,
        [FromBody] UpdateClientStatusRequest body)
    {
        if (!CurrentUser.IsPlatformAdmin(User)) return Forbid();
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == clientId);
        if (client is null) return NotFound(new ErrorResponse("NOT_FOUND"));
        client.Status = body.Status;
        await _db.SaveChangesAsync();
        return Ok(new ClientStatusResponse(client.Id, client.Status));
    }
}
