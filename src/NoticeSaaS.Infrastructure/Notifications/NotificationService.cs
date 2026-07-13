using Microsoft.EntityFrameworkCore;
using NoticeSaaS.Application.Notifications;
using NoticeSaaS.Infrastructure.Persistence;

namespace NoticeSaaS.Infrastructure.Notifications;

public sealed class NotificationService(NoticeSaaSDbContext db) : INotificationService
{
    public async Task<NotificationListResponse> ListAsync(
        Guid organizationId,
        Guid userId,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 50);
        var query = db.Notifications.AsNoTracking()
            .Where(n => n.OrganizationId == organizationId && n.UserId == userId);

        var unread = await query.CountAsync(n => !n.IsRead, cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .Select(n => new NotificationDto(
                n.Id,
                n.Title,
                n.Body,
                n.IsRead,
                n.NoticeId,
                n.ReminderId,
                n.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new NotificationListResponse(unread, items);
    }

    public Task<int> UnreadCountAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        db.Notifications.AsNoTracking()
            .CountAsync(
                n => n.OrganizationId == organizationId && n.UserId == userId && !n.IsRead,
                cancellationToken);

    public async Task<bool> MarkReadAsync(
        Guid organizationId,
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var updated = await db.Notifications
            .Where(n =>
                n.Id == notificationId
                && n.OrganizationId == organizationId
                && n.UserId == userId
                && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAtUtc, DateTimeOffset.UtcNow),
                cancellationToken);

        return updated > 0;
    }

    public Task<int> MarkAllReadAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        db.Notifications
            .Where(n => n.OrganizationId == organizationId && n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAtUtc, DateTimeOffset.UtcNow),
                cancellationToken);
}
