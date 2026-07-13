namespace NoticeSaaS.Application.Reminders;

public sealed record ReminderListItemDto(
    Guid Id,
    Guid? NoticeId,
    Guid? ClientId,
    string Module,
    string Description,
    string? ProceedingId,
    string? DocumentReferenceId,
    string? AssesseeIdentifier,
    string Priority,
    DateOnly DueOn,
    bool IsDone,
    bool IsOverdue,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record ReminderListResponse(
    int PendingCount,
    int DoneCount,
    IReadOnlyList<ReminderListItemDto> Reminders);

public sealed record CreateReminderRequest(
    Guid? NoticeId,
    string Description,
    string Priority,
    DateOnly DueOn);

public interface IReminderService
{
    Task<ReminderListResponse> ListAsync(
        Guid organizationId,
        bool? isDone,
        string? priority,
        string? search,
        CancellationToken cancellationToken = default);

    Task<ReminderListItemDto?> CreateAsync(
        Guid organizationId,
        Guid userId,
        CreateReminderRequest request,
        CancellationToken cancellationToken = default);

    Task<ReminderListItemDto?> CompleteAsync(
        Guid organizationId,
        Guid reminderId,
        CancellationToken cancellationToken = default);
}
