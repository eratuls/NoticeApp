namespace NoticeSaaS.Application.Sync;

public sealed record SyncJobDto(
    Guid Id,
    Guid ClientId,
    string Status,
    string Trigger,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? ErrorMessage,
    int NoticesUpserted,
    IReadOnlyList<SyncJobLogDto> Logs);

public sealed record SyncJobLogDto(
    DateTimeOffset AtUtc,
    string Level,
    string Message);

public sealed record TriggerSyncResult(bool Succeeded, SyncJobDto? Job, string? Error)
{
    public static TriggerSyncResult Ok(SyncJobDto job) => new(true, job, null);
    public static TriggerSyncResult Fail(string error) => new(false, null, error);
}

public interface ISyncService
{
    Task<TriggerSyncResult> TriggerManualAsync(
        Guid organizationId,
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<SyncJobDto?> GetLatestForClientAsync(
        Guid organizationId,
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<int> EnqueueDueScheduledAsync(CancellationToken cancellationToken = default);
}

public interface ISyncJobProcessor
{
    Task ProcessJobAsync(Guid syncJobId, CancellationToken cancellationToken = default);

    Task ProcessPendingAsync(int maxJobs, CancellationToken cancellationToken = default);
}

/// <summary>Fetches notices from the Income Tax portal for password-only accounts.</summary>
public interface IIncomeTaxPortalClient
{
    Task<IReadOnlyList<PortalNoticeDto>> FetchNoticesAsync(
        PortalLoginRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record PortalLoginRequest(
    string Username,
    string Password,
    string Pan,
    string ClientName);

public sealed record PortalNoticeDto(
    string Section,
    string Description,
    string? FinancialYear,
    string? ProceedingId,
    string DocumentReferenceId,
    string Kind,
    DateOnly? ServedDate,
    DateOnly? ResponseDueDate,
    string? PdfUrl);
