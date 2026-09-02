using GateFlow.Application.Abstractions;
using GateFlow.Contracts.Access;
using GateFlow.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GateFlow.Api.Controllers;

[ApiController]
[Route("v1/access")]
[Tags("Access (Device)")]
public sealed class AccessController : ControllerBase
{
    private readonly IAccessService _access;

    public AccessController(IAccessService access) => _access = access;

    [HttpPost("check")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AccessCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AccessCheckResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccessCheckResponse>> Check(
        [FromBody] AccessCheckRequest body,
        [FromHeader(Name = "X-Device-Key")] string? deviceApiKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.CredentialType) || string.IsNullOrWhiteSpace(body.Code))
            return BadRequest(new ErrorResponse("INVALID_BODY"));

        var result = await _access.CheckAsync(body, deviceApiKey, ct);
        if (!result.Open)
            return StatusCode(StatusCodes.Status403Forbidden, result);
        return Ok(result);
    }
}
