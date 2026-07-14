using Microsoft.EntityFrameworkCore;
using NoticeSaaS.Application.Notices;
using NoticeSaaS.Domain.Entities;
using NoticeSaaS.Domain.Enums;
using NoticeSaaS.Infrastructure.Persistence;

namespace NoticeSaaS.Infrastructure.Notices;

public sealed class NoticeService(NoticeSaaSDbContext db, INoticeAttachmentStorage storage) : INoticeService
{
    public async Task<ClientNoticesResponse?> ListForClientAsync(
        Guid organizationId,
        Guid clientId,
        string? kind,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var client = await db.Clients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clientId && c.OrganizationId == organizationId, cancellationToken);
        if (client is null)
        {
            return null;
        }

        var query = db.Notices.AsNoTracking()
            .Where(n => n.OrganizationId == organizationId && n.ClientId == clientId);

        if (!string.IsNullOrWhiteSpace(kind)
            && Enum.TryParse<NoticeKind>(kind, ignoreCase: true, out var parsedKind))
        {
            query = query.Where(n => n.Kind == parsedKind);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(n =>
                n.Section.Contains(term)
                || n.Description.Contains(term)
                || (n.DocumentReferenceId != null && n.DocumentReferenceId.Contains(term))
                || (n.ProceedingId != null && n.ProceedingId.Contains(term)));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var notices = await query
            .Include(n => n.AssignedTo)
            .Include(n => n.Attachments)
            .OrderByDescending(n => n.ServedDate)
            .ThenByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var items = notices.Select(n => new NoticeListItemDto(
            n.Id,
            n.ClientId,
            n.Kind.ToString(),
            n.Section,
            n.Description,
            n.FinancialYear,
            n.ProceedingId,
            n.DocumentReferenceId,
            n.Status.ToString(),
            n.Status != NoticeWorkflowStatus.Closed
                && n.ResponseDueDate != null
                && n.ResponseDueDate < today,
            n.ServedDate,
            n.ResponseDueDate,
            n.CreatedAtUtc,
            n.AssignedTo is null ? null : $"{n.AssignedTo.FirstName} {n.AssignedTo.LastName}".Trim(),
            n.Attachments.Any(a => a.Category == "NoticeDocument"))).ToList();

        var kindCounts = await db.Notices.AsNoTracking()
            .Where(n => n.OrganizationId == organizationId && n.ClientId == clientId)
            .GroupBy(n => n.Kind)
            .Select(g => new { Kind = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Kind, x => x.Count, cancellationToken);

        foreach (var name in Enum.GetNames<NoticeKind>())
        {
            kindCounts.TryAdd(name, 0);
        }

        return new ClientNoticesResponse(
            client.Id,
            client.Name,
            client.Pan,
            client.IsActive,
            kindCounts,
            items);
    }

    public async Task<NoticeDetailDto?> GetAsync(
        Guid organizationId,
        Guid noticeId,
        CancellationToken cancellationToken = default)
    {
        var notice = await db.Notices.AsNoTracking()
            .Include(n => n.Client)
            .Include(n => n.AssignedTo)
            .Include(n => n.Comments).ThenInclude(c => c.Author)
            .Include(n => n.StatusEvents)
            .Include(n => n.Attachments).ThenInclude(a => a.UploadedBy)
            .FirstOrDefaultAsync(n => n.Id == noticeId && n.OrganizationId == organizationId, cancellationToken);

        return notice is null ? null : MapDetail(notice);
    }

    public async Task<NoticeDetailDto?> UpdateStatusAsync(
        Guid organizationId,
        Guid noticeId,
        Guid userId,
        UpdateNoticeStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<NoticeWorkflowStatus>(request.Status, ignoreCase: true, out var toStatus))
        {
            return null;
        }

        var notice = await db.Notices
            .FirstOrDefaultAsync(n => n.Id == noticeId && n.OrganizationId == organizationId, cancellationToken);

        if (notice is null)
        {
            return null;
        }

        var from = notice.Status;
        if (from != toStatus)
        {
            var closedAt = toStatus == NoticeWorkflowStatus.Closed ? DateTimeOffset.UtcNow : (DateTimeOffset?)null;
            var updated = await db.Notices
                .Where(n => n.Id == noticeId && n.OrganizationId == organizationId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(n => n.Status, toStatus)
                        .SetProperty(n => n.ClosedAtUtc, closedAt),
                    cancellationToken);

            if (updated == 0)
            {
                return null;
            }

            db.NoticeStatusEvents.Add(new NoticeStatusEvent
            {
                Id = Guid.NewGuid(),
                NoticeId = notice.Id,
                FromStatus = from,
                ToStatus = toStatus,
                ChangedByUserId = userId,
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
        }

        return await GetAsync(organizationId, noticeId, cancellationToken);
    }

    public async Task<NoticeCommentDto?> AddCommentAsync(
        Guid organizationId,
        Guid noticeId,
        Guid userId,
        AddNoticeCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var body = request.Body?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        var noticeExists = await db.Notices.AnyAsync(
            n => n.Id == noticeId && n.OrganizationId == organizationId,
            cancellationToken);
        if (!noticeExists)
        {
            return null;
        }

        var author = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (author is null)
        {
            return null;
        }

        var comment = new NoticeComment
        {
            Id = Guid.NewGuid(),
            NoticeId = noticeId,
            AuthorUserId = userId,
            Body = body,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.NoticeComments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);

        return new NoticeCommentDto(
            comment.Id,
            $"{author.FirstName} {author.LastName}".Trim(),
            comment.Body,
            comment.CreatedAtUtc);
    }

    public async Task<NoticeDetailDto?> AssignAsync(
        Guid organizationId,
        Guid noticeId,
        AssignNoticeRequest request,
        CancellationToken cancellationToken = default)
    {
        var notice = await db.Notices
            .FirstOrDefaultAsync(n => n.Id == noticeId && n.OrganizationId == organizationId, cancellationToken);
        if (notice is null)
        {
            return null;
        }

        if (request.AssignedToUserId is Guid assigneeId)
        {
            var memberOk = await db.OrganizationMembers.AnyAsync(
                m => m.OrganizationId == organizationId && m.UserId == assigneeId,
                cancellationToken);
            if (!memberOk)
            {
                return null;
            }

            notice.AssignedToUserId = assigneeId;
        }
        else
        {
            notice.AssignedToUserId = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(organizationId, noticeId, cancellationToken);
    }

    public async Task<NoticeListItemDto?> CreateManualAsync(
        Guid organizationId,
        Guid clientId,
        CreateManualNoticeRequest request,
        CancellationToken cancellationToken = default)
    {
        var section = request.Section?.Trim() ?? string.Empty;
        var description = request.Description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var clientExists = await db.Clients.AnyAsync(
            c => c.Id == clientId && c.OrganizationId == organizationId,
            cancellationToken);
        if (!clientExists)
        {
            return null;
        }

        var notice = new Notice
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ClientId = clientId,
            Module = ComplianceModule.IncomeTax,
            Kind = NoticeKind.Manual,
            Section = section,
            Description = description,
            FinancialYear = string.IsNullOrWhiteSpace(request.FinancialYear) ? null : request.FinancialYear.Trim(),
            DocumentReferenceId = string.IsNullOrWhiteSpace(request.DocumentReferenceId)
                ? null
                : request.DocumentReferenceId.Trim(),
            Status = NoticeWorkflowStatus.New,
            ServedDate = request.ServedDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            ResponseDueDate = request.ResponseDueDate,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.Notices.Add(notice);
        await db.SaveChangesAsync(cancellationToken);

        return new NoticeListItemDto(
            notice.Id,
            notice.ClientId,
            notice.Kind.ToString(),
            notice.Section,
            notice.Description,
            notice.FinancialYear,
            notice.ProceedingId,
            notice.DocumentReferenceId,
            notice.Status.ToString(),
            false,
            notice.ServedDate,
            notice.ResponseDueDate,
            notice.CreatedAtUtc,
            null,
            false);
    }

    public async Task<NoticeAttachmentDto?> UploadAttachmentAsync(
        Guid organizationId,
        Guid noticeId,
        Guid userId,
        string category,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var normalizedCategory = NormalizeCategory(category);
        if (normalizedCategory is null || string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var noticeExists = await db.Notices.AnyAsync(
            n => n.Id == noticeId && n.OrganizationId == organizationId,
            cancellationToken);
        if (!noticeExists)
        {
            return null;
        }

        var uploader = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (uploader is null)
        {
            return null;
        }

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var size = buffer.Length;
        if (size <= 0 || size > 10 * 1024 * 1024)
        {
            return null;
        }

        buffer.Position = 0;
        var stored = await storage.SaveAsync(noticeId, fileName, buffer, cancellationToken);
        var attachment = new NoticeAttachment
        {
            Id = Guid.NewGuid(),
            NoticeId = noticeId,
            Category = normalizedCategory,
            FileName = Path.GetFileName(fileName),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            StoredFileName = stored,
            SizeBytes = size,
            UploadedByUserId = userId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.NoticeAttachments.Add(attachment);
        await db.SaveChangesAsync(cancellationToken);

        return MapAttachment(attachment, $"{uploader.FirstName} {uploader.LastName}".Trim());
    }

    public async Task<(Stream Stream, string ContentType, string FileName)?> OpenAttachmentAsync(
        Guid organizationId,
        Guid noticeId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await db.NoticeAttachments.AsNoTracking()
            .Include(a => a.Notice)
            .FirstOrDefaultAsync(
                a => a.Id == attachmentId && a.NoticeId == noticeId && a.Notice.OrganizationId == organizationId,
                cancellationToken);
        if (attachment is null)
        {
            return null;
        }

        var stream = await storage.OpenReadAsync(attachment.StoredFileName, cancellationToken);
        if (stream is null)
        {
            return null;
        }

        return (stream, attachment.ContentType, attachment.FileName);
    }

    private static string? NormalizeCategory(string? category) =>
        category?.Trim() switch
        {
            "NoticeDocument" or "notice" or "Notice" or "pdf" => "NoticeDocument",
            "Reply" or "reply" => "Reply",
            _ => null
        };

    private static NoticeAttachmentDto MapAttachment(NoticeAttachment a, string uploaderName) =>
        new(
            a.Id,
            a.Category,
            a.FileName,
            a.ContentType,
            a.SizeBytes,
            uploaderName,
            a.CreatedAtUtc,
            $"/api/v1/notices/{a.NoticeId}/attachments/{a.Id}/download");

    private static NoticeDetailDto MapDetail(Notice notice)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new NoticeDetailDto(
            notice.Id,
            notice.ClientId,
            notice.Client.Name,
            notice.Client.Pan,
            notice.Kind.ToString(),
            notice.Module.ToString(),
            notice.Section,
            notice.Description,
            notice.FinancialYear,
            notice.ProceedingId,
            notice.DocumentReferenceId,
            notice.Status.ToString(),
            notice.IsOverdue(today),
            notice.ServedDate,
            notice.ResponseDueDate,
            notice.ResponseSubmittedDate,
            notice.CreatedAtUtc,
            notice.AssignedToUserId,
            notice.AssignedTo is null
                ? null
                : $"{notice.AssignedTo.FirstName} {notice.AssignedTo.LastName}".Trim(),
            notice.Comments
                .OrderByDescending(c => c.CreatedAtUtc)
                .Select(c => new NoticeCommentDto(
                    c.Id,
                    $"{c.Author.FirstName} {c.Author.LastName}".Trim(),
                    c.Body,
                    c.CreatedAtUtc))
                .ToList(),
            notice.StatusEvents
                .OrderByDescending(e => e.CreatedAtUtc)
                .Select(e => new NoticeStatusEventDto(
                    e.Id,
                    e.FromStatus?.ToString(),
                    e.ToStatus.ToString(),
                    e.Note,
                    e.CreatedAtUtc))
                .ToList(),
            notice.Attachments
                .OrderByDescending(a => a.CreatedAtUtc)
                .Select(a => MapAttachment(
                    a,
                    $"{a.UploadedBy.FirstName} {a.UploadedBy.LastName}".Trim()))
                .ToList());
    }
}
