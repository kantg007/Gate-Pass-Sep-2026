using GateFlow.Application.Abstractions;
using GateFlow.Application.Security;
using GateFlow.Contracts.Auth;
using GateFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Controllers;

[ApiController]
[Route("v1/auth")]
[Tags("Auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly GateFlowDbContext _db;

    public AuthController(IAuthService auth, GateFlowDbContext db)
    {
        _auth = auth;
        _db = db;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.CompanyName) ||
            string.IsNullOrWhiteSpace(body.FullName) ||
            string.IsNullOrWhiteSpace(body.Email) ||
            string.IsNullOrWhiteSpace(body.Password))
        {
            return BadRequest(new ErrorResponse("INVALID_BODY"));
        }

        var (ok, error, payload) = await _auth.RegisterClientAsync(
            body.CompanyName, body.FullName, body.Email, body.Password, body.Phone);
        if (!ok || payload is null)
            return Conflict(new ErrorResponse(error ?? "CONFLICT"));
        return StatusCode(StatusCodes.Status201Created, payload);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest body)
    {
        var (ok, error, payload) = await _auth.LoginAsync(body.Email, body.Password);
        if (!ok || payload is null)
            return Unauthorized(new ErrorResponse(error ?? "INVALID_CREDENTIALS"));
        return Ok(payload);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthUserDto>> Me()
    {
        var id = CurrentUser.UserId(User);
        if (id is null) return Unauthorized();
        var user = await _db.Users.Include(u => u.Client).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return Unauthorized();
        return Ok(new AuthUserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            user.ClientId,
            user.SiteId,
            user.Client == null ? null : new ClientSummaryDto(user.Client.Id, user.Client.Name, user.Client.Status)));
    }
}
