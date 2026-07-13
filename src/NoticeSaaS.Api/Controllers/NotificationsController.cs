using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoticeSaaS.Application.Notifications;

namespace NoticeSaaS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        if (organizationId is null || userId is null)
        {
            return Unauthorized();
        }

        var result = await notificationService.ListAsync(
            organizationId.Value,
            userId.Value,
            take,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        if (organizationId is null || userId is null)
        {
            return Unauthorized();
        }

        var count = await notificationService.UnreadCountAsync(
            organizationId.Value,
            userId.Value,
            cancellationToken);

        return Ok(new { unreadCount = count });
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        if (organizationId is null || userId is null)
        {
            return Unauthorized();
        }

        await notificationService.MarkReadAsync(
            organizationId.Value,
            userId.Value,
            notificationId,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        if (organizationId is null || userId is null)
        {
            return Unauthorized();
        }

        var count = await notificationService.MarkAllReadAsync(
            organizationId.Value,
            userId.Value,
            cancellationToken);

        return Ok(new { marked = count });
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
