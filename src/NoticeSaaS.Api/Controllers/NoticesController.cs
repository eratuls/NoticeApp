using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoticeSaaS.Application.Notices;

namespace NoticeSaaS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public class NoticesController(INoticeService noticeService) : ControllerBase
{
    [HttpGet("clients/{clientId:guid}/notices")]
    public async Task<IActionResult> ListForClient(
        Guid clientId,
        [FromQuery] string? kind = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await noticeService.ListForClientAsync(
            organizationId.Value,
            clientId,
            kind,
            search,
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("notices/{noticeId:guid}")]
    public async Task<IActionResult> Get(Guid noticeId, CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await noticeService.GetAsync(organizationId.Value, noticeId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("notices/{noticeId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid noticeId,
        [FromBody] UpdateNoticeStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        if (organizationId is null || userId is null)
        {
            return Unauthorized();
        }

        var result = await noticeService.UpdateStatusAsync(
            organizationId.Value,
            noticeId,
            userId.Value,
            request,
            cancellationToken);

        return result is null ? BadRequest(new { message = "Unable to update status." }) : Ok(result);
    }

    [HttpPost("notices/{noticeId:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid noticeId,
        [FromBody] AddNoticeCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        if (organizationId is null || userId is null)
        {
            return Unauthorized();
        }

        var result = await noticeService.AddCommentAsync(
            organizationId.Value,
            noticeId,
            userId.Value,
            request,
            cancellationToken);

        return result is null ? BadRequest(new { message = "Unable to add comment." }) : Ok(result);
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
