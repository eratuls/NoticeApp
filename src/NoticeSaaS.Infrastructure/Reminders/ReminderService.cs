using Microsoft.EntityFrameworkCore;
using NoticeSaaS.Application.Notifications;
using NoticeSaaS.Application.Reminders;
using NoticeSaaS.Domain.Entities;
using NoticeSaaS.Domain.Enums;
using NoticeSaaS.Infrastructure.Persistence;

namespace NoticeSaaS.Infrastructure.Reminders;

public sealed class ReminderService(NoticeSaaSDbContext db) : IReminderService
{
    public async Task<ReminderListResponse> ListAsync(
        Guid organizationId,
        bool? isDone,
        string? priority,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = db.Reminders.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId);

        var pendingCount = await baseQuery.CountAsync(r => !r.IsDone, cancellationToken);
        var doneCount = await baseQuery.CountAsync(r => r.IsDone, cancellationToken);

        var query = baseQuery;
        if (isDone is not null)
        {
            query = query.Where(r => r.IsDone == isDone.Value);
        }

        if (!string.IsNullOrWhiteSpace(priority)
            && Enum.TryParse<ReminderPriority>(priority, ignoreCase: true, out var parsedPriority))
        {
            query = query.Where(r => r.Priority == parsedPriority);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                (r.ProceedingId != null && r.ProceedingId.Contains(term))
                || (r.DocumentReferenceId != null && r.DocumentReferenceId.Contains(term))
                || (r.AssesseeIdentifier != null && r.AssesseeIdentifier.Contains(term))
                || r.Description.Contains(term));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reminders = await query
            .OrderBy(r => r.IsDone)
            .ThenBy(r => r.DueOn)
            .ThenByDescending(r => r.Priority)
            .Select(r => new ReminderListItemDto(
                r.Id,
                r.NoticeId,
                r.ClientId,
                r.Module.ToString(),
                r.Description,
                r.ProceedingId,
                r.DocumentReferenceId,
                r.AssesseeIdentifier,
                r.Priority.ToString(),
                r.DueOn,
                r.IsDone,
                !r.IsDone && r.DueOn < today,
                r.CreatedAtUtc,
                r.CompletedAtUtc))
            .ToListAsync(cancellationToken);

        return new ReminderListResponse(pendingCount, doneCount, reminders);
    }

    public async Task<ReminderListItemDto?> CreateAsync(
        Guid organizationId,
        Guid userId,
        CreateReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        var description = request.Description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(description)
            || !Enum.TryParse<ReminderPriority>(request.Priority, ignoreCase: true, out var priority))
        {
            return null;
        }

        Notice? notice = null;
        if (request.NoticeId is Guid noticeId)
        {
            notice = await db.Notices
                .Include(n => n.Client)
                .FirstOrDefaultAsync(
                    n => n.Id == noticeId && n.OrganizationId == organizationId,
                    cancellationToken);
            if (notice is null)
            {
                return null;
            }
        }

        var now = DateTimeOffset.UtcNow;
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NoticeId = notice?.Id,
            ClientId = notice?.ClientId,
            Module = notice?.Module ?? ComplianceModule.IncomeTax,
            Description = description,
            ProceedingId = notice?.ProceedingId,
            DocumentReferenceId = notice?.DocumentReferenceId,
            AssesseeIdentifier = notice?.Client.Pan,
            Priority = priority,
            DueOn = request.DueOn,
            IsDone = false,
            CreatedByUserId = userId,
            CreatedAtUtc = now
        };

        db.Reminders.Add(reminder);
        db.Notifications.Add(new AppNotification
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            Title = "Reminder scheduled",
            Body = $"{description} · due {request.DueOn:yyyy-MM-dd}",
            IsRead = false,
            NoticeId = reminder.NoticeId,
            ReminderId = reminder.Id,
            CreatedAtUtc = now
        });

        await db.SaveChangesAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new ReminderListItemDto(
            reminder.Id,
            reminder.NoticeId,
            reminder.ClientId,
            reminder.Module.ToString(),
            reminder.Description,
            reminder.ProceedingId,
            reminder.DocumentReferenceId,
            reminder.AssesseeIdentifier,
            reminder.Priority.ToString(),
            reminder.DueOn,
            reminder.IsDone,
            !reminder.IsDone && reminder.DueOn < today,
            reminder.CreatedAtUtc,
            reminder.CompletedAtUtc);
    }

    public async Task<ReminderListItemDto?> CompleteAsync(
        Guid organizationId,
        Guid reminderId,
        CancellationToken cancellationToken = default)
    {
        var reminder = await db.Reminders
            .FirstOrDefaultAsync(r => r.Id == reminderId && r.OrganizationId == organizationId, cancellationToken);
        if (reminder is null)
        {
            return null;
        }

        if (!reminder.IsDone)
        {
            reminder.IsDone = true;
            reminder.CompletedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new ReminderListItemDto(
            reminder.Id,
            reminder.NoticeId,
            reminder.ClientId,
            reminder.Module.ToString(),
            reminder.Description,
            reminder.ProceedingId,
            reminder.DocumentReferenceId,
            reminder.AssesseeIdentifier,
            reminder.Priority.ToString(),
            reminder.DueOn,
            reminder.IsDone,
            false,
            reminder.CreatedAtUtc,
            reminder.CompletedAtUtc);
    }
}
