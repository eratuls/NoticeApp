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

    [HttpPost("clients/{clientId:guid}/notices/manual")]
    public async Task<IActionResult> CreateManual(
        Guid clientId,
        [FromBody] CreateManualNoticeRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await noticeService.CreateManualAsync(
            organizationId.Value,
            clientId,
            request,
            cancellationToken);

        return result is null
            ? BadRequest(new { message = "Unable to create manual notice." })
            : Ok(result);
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

    [HttpPatch("notices/{noticeId:guid}/assign")]
    public async Task<IActionResult> Assign(
        Guid noticeId,
        [FromBody] AssignNoticeRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await noticeService.AssignAsync(organizationId.Value, noticeId, request, cancellationToken);
        return result is null
            ? BadRequest(new { message = "Unable to assign notice. Member must belong to this workspace." })
            : Ok(result);
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

    [HttpPost("notices/{noticeId:guid}/attachments")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadAttachment(
        Guid noticeId,
        [FromForm] string category,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        if (organizationId is null || userId is null)
        {
            return Unauthorized();
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "A file is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await noticeService.UploadAttachmentAsync(
            organizationId.Value,
            noticeId,
            userId.Value,
            category,
            file.FileName,
            file.ContentType,
            stream,
            cancellationToken);

        return result is null
            ? BadRequest(new { message = "Unable to upload attachment. Use category NoticeDocument or Reply." })
            : Ok(result);
    }

    [HttpGet("notices/{noticeId:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment(
        Guid noticeId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await noticeService.OpenAttachmentAsync(
            organizationId.Value,
            noticeId,
            attachmentId,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
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
