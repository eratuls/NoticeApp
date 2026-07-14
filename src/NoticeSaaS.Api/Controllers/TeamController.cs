using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoticeSaaS.Application.Team;

namespace NoticeSaaS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/team")]
public class TeamController(ITeamService teamService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? role = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await teamService.ListAsync(organizationId.Value, role, search, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Add(
        [FromBody] AddTeamMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await teamService.AddAsync(organizationId.Value, request, cancellationToken);
        return result.Succeeded
            ? Ok(new { result.Member, result.TemporaryPassword })
            : BadRequest(new { message = result.Error });
    }

    private Guid? GetOrganizationId()
    {
        var value = User.FindFirstValue("org_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
