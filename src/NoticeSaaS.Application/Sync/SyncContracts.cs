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
    DateTimeOffset? OtpRequestedAtUtc,
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

public sealed record SubmitOtpResult(bool Succeeded, SyncJobDto? Job, string? Error)
{
    public static SubmitOtpResult Ok(SyncJobDto job) => new(true, job, null);
    public static SubmitOtpResult Fail(string error) => new(false, null, error);
}

public interface ISyncService
{
    Task<TriggerSyncResult> TriggerManualAsync(
        Guid organizationId,
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<SubmitOtpResult> SubmitOtpAsync(
        Guid organizationId,
        Guid clientId,
        Guid syncJobId,
        string otp,
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

/// <summary>Income Tax e-Filing portal adapter (login/profile + notice fetch).</summary>
public interface IIncomeTaxPortalClient
{
    /// <summary>
    /// Validates portal credentials and returns assessee profile (name, PAN, masked Aadhaar).
    /// Does not require vault OTP — used when adding a client.
    /// </summary>
    Task<PortalProfileDto> LoginAndGetProfileAsync(
        PortalCredentialsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches notices (password-only or OTP-assisted after vault challenge).</summary>
    Task<IReadOnlyList<PortalNoticeDto>> FetchNoticesAsync(
        PortalLoginRequest request,
        string? otp = null,
        CancellationToken cancellationToken = default);
}

public sealed record PortalCredentialsRequest(string Username, string Password);

public sealed record PortalProfileDto(
    string Name,
    string Pan,
    string AadhaarMasked);

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

/// <summary>Thrown when the portal account has e-Filing Vault enabled and OTP is required.</summary>
public sealed class PortalOtpRequiredException : Exception
{
    public PortalOtpRequiredException()
        : base("Income Tax portal requires vault OTP to continue login.")
    {
    }
}

/// <summary>Invalid portal credentials or OTP — do not retry.</summary>
public sealed class PortalAuthException : Exception
{
    public PortalAuthException(string message)
        : base(message)
    {
    }
}

/// <summary>Transient portal failure (timeout / unavailable) — safe to retry.</summary>
public sealed class PortalTransientException : Exception
{
    public PortalTransientException(string message)
        : base(message)
    {
    }

    public PortalTransientException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>Defaults for portal call boundaries in the sync worker.</summary>
public static class PortalCallDefaults
{
    public static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(30);
    public const int MaxAttempts = 3;
}
