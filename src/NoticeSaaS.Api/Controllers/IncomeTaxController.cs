using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoticeSaaS.Application.Sync;

namespace NoticeSaaS.Api.Controllers;

/// <summary>Income Tax e-Filing portal APIs (credential validation + profile).</summary>
[ApiController]
[Authorize]
[Route("api/v1/income-tax")]
public class IncomeTaxController(IIncomeTaxPortalClient portalClient) : ControllerBase
{
    /// <summary>
    /// Validates Income Tax portal username/password and returns PAN, masked Aadhaar, and name.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] IncomeTaxLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        try
        {
            var profile = await portalClient.LoginAndGetProfileAsync(
                new PortalCredentialsRequest(request.Username.Trim(), request.Password),
                cancellationToken);

            return Ok(new IncomeTaxProfileResponse(
                profile.Name,
                profile.Pan,
                profile.AadhaarMasked));
        }
        catch (Exception ex) when (ex is InvalidOperationException or PortalAuthException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public sealed record IncomeTaxLoginRequest(string Username, string Password);

public sealed record IncomeTaxProfileResponse(string Name, string Pan, string AadhaarMasked);
