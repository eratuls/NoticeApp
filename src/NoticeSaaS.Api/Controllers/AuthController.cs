using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoticeSaaS.Application.Auth;

namespace NoticeSaaS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await authService.LoginAsync(request, ip, userAgent, cancellationToken);

        if (result.Conflict && result.ConflictDetails is not null)
        {
            return Conflict(result.ConflictDetails);
        }

        if (!result.Succeeded || result.Success is null)
        {
            return Unauthorized(new { message = result.Error ?? "Invalid email or password." });
        }

        return Ok(result.Success);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var sessionId = GetSessionId();
        if (sessionId is null)
        {
            return Unauthorized();
        }

        await authService.LogoutAsync(sessionId.Value, cancellationToken);
        return NoContent();
    }

    [HttpGet("session")]
    [Authorize]
    public async Task<IActionResult> Session(CancellationToken cancellationToken)
    {
        var sessionId = GetSessionId();
        if (sessionId is null)
        {
            return Unauthorized();
        }

        var status = await authService.GetSessionStatusAsync(sessionId.Value, cancellationToken);
        if (status is null)
        {
            return Unauthorized(new { code = "SESSION_EXPIRED", message = "Session is no longer active." });
        }

        return Ok(status);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var sessionId = GetSessionId();
        if (sessionId is null)
        {
            return Unauthorized();
        }

        var status = await authService.GetSessionStatusAsync(sessionId.Value, cancellationToken);
        return status is null ? Unauthorized() : Ok(status.User);
    }

    private Guid? GetSessionId()
    {
        var value = User.FindFirstValue("session_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
