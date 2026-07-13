using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoticeSaaS.Application.Reminders;

namespace NoticeSaaS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/reminders")]
public class RemindersController(IReminderService reminderService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        bool? isDone = status?.ToLowerInvariant() switch
        {
            "pending" => false,
            "done" => true,
            _ => null
        };

        var result = await reminderService.ListAsync(
            organizationId.Value,
            isDone,
            priority,
            search,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        if (organizationId is null || userId is null)
        {
            return Unauthorized();
        }

        var result = await reminderService.CreateAsync(
            organizationId.Value,
            userId.Value,
            request,
            cancellationToken);

        return result is null
            ? BadRequest(new { message = "Unable to create reminder." })
            : Ok(result);
    }

    [HttpPost("{reminderId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid reminderId, CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await reminderService.CompleteAsync(organizationId.Value, reminderId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private Guid? GetOrganizationId()
    {
        var value = User.FindFirstValue("org_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
