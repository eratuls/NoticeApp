namespace NoticeSaaS.Application.Notifications;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    bool IsRead,
    Guid? NoticeId,
    Guid? ReminderId,
    DateTimeOffset CreatedAtUtc);

public sealed record NotificationListResponse(
    int UnreadCount,
    IReadOnlyList<NotificationDto> Notifications);

public interface INotificationService
{
    Task<NotificationListResponse> ListAsync(
        Guid organizationId,
        Guid userId,
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<int> UnreadCountAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> MarkReadAsync(
        Guid organizationId,
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
