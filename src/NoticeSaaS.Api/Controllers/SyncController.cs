using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoticeSaaS.Application.Sync;

namespace NoticeSaaS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/clients/{clientId:guid}/sync")]
public class SyncController(ISyncService syncService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Trigger(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await syncService.TriggerManualAsync(organizationId.Value, clientId, cancellationToken);
        if (!result.Succeeded || result.Job is null)
        {
            return BadRequest(new { message = result.Error ?? "Unable to start sync." });
        }

        return Ok(result.Job);
    }

    [HttpGet]
    public async Task<IActionResult> Latest(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var job = await syncService.GetLatestForClientAsync(organizationId.Value, clientId, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    private Guid? GetOrganizationId()
    {
        var value = User.FindFirstValue("org_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
