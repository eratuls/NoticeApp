using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoticeSaaS.Application.Billing;
using NoticeSaaS.Application.Sync;
using NoticeSaaS.Domain.Entities;
using NoticeSaaS.Domain.Enums;
using NoticeSaaS.Infrastructure.Clients;
using NoticeSaaS.Infrastructure.Persistence;

namespace NoticeSaaS.Infrastructure.Sync;

public sealed class SyncJobProcessor(
    NoticeSaaSDbContext db,
    IDataProtectionProvider dataProtectionProvider,
    IIncomeTaxPortalClient portalClient,
    IUsageLimitsService usageLimitsService,
    ILogger<SyncJobProcessor> logger) : ISyncJobProcessor
{
    /// <summary>How long a job may wait for vault OTP before failing.</summary>
    public static readonly TimeSpan OtpTimeout = TimeSpan.FromMinutes(5);

    private readonly IDataProtector _credentialProtector =
        dataProtectionProvider.CreateProtector("NoticeSaaS.PortalCredentials.v1");

    private readonly IDataProtector _otpProtector =
        dataProtectionProvider.CreateProtector("NoticeSaaS.SyncJobOtp.v1");

    public async Task ProcessPendingAsync(int maxJobs, CancellationToken cancellationToken = default)
    {
        await FailTimedOutOtpJobsAsync(cancellationToken);

        var jobIds = await db.SyncJobs
            .AsNoTracking()
            .Where(j => j.Status == SyncJobStatus.Pending
                        || (j.Status == SyncJobStatus.AwaitingOtp && j.SubmittedOtpProtected != null))
            .OrderBy(j => j.CreatedAtUtc)
            .Take(maxJobs)
            .Select(j => j.Id)
            .ToListAsync(cancellationToken);

        foreach (var jobId in jobIds)
        {
            await ProcessJobAsync(jobId, cancellationToken);
        }
    }

    public async Task ProcessJobAsync(Guid syncJobId, CancellationToken cancellationToken = default)
    {
        db.ChangeTracker.Clear();

        var job = await db.SyncJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == syncJobId, cancellationToken);

        if (job is null)
        {
            return;
        }

        if (job.Status is SyncJobStatus.Succeeded or SyncJobStatus.Failed or SyncJobStatus.Running)
        {
            return;
        }

        if (job.Status == SyncJobStatus.AwaitingOtp && string.IsNullOrEmpty(job.SubmittedOtpProtected))
        {
            return;
        }

        var client = await db.Clients.AsNoTracking()
            .Include(c => c.Credential)
            .FirstOrDefaultAsync(c => c.Id == job.ClientId, cancellationToken);

        if (client is null)
        {
            await MarkFailedAsync(syncJobId, "Client not found.", cancellationToken);
            return;
        }

        if (client.Credential is null)
        {
            await MarkFailedAsync(syncJobId, "Portal credentials are missing for this client.", cancellationToken);
            return;
        }

        var creditCheck = await usageLimitsService.EnsureHasSyncCreditAsync(job.OrganizationId, cancellationToken);
        if (!creditCheck.Allowed)
        {
            await MarkFailedAsync(syncJobId, creditCheck.Error ?? "Sync credits exhausted.", cancellationToken);
            return;
        }

        string? otp = null;
        if (!string.IsNullOrEmpty(job.SubmittedOtpProtected))
        {
            try
            {
                otp = _otpProtector.Unprotect(job.SubmittedOtpProtected);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to decrypt submitted OTP for sync job {JobId}", syncJobId);
                await MarkFailedAsync(syncJobId, "Unable to read submitted OTP.", cancellationToken);
                return;
            }
        }

        await MarkRunningAsync(syncJobId, job.Status, cancellationToken);

        try
        {
            string password;
            try
            {
                password = _credentialProtector.Unprotect(client.Credential.PasswordProtected);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to decrypt portal credentials for client {ClientId}", client.Id);
                await MarkFailedAsync(syncJobId, "Unable to decrypt portal credentials.", cancellationToken);
                return;
            }

            await AddLogAsync(syncJobId, "Info",
                otp is null
                    ? "Logging in to Income Tax portal (credentials decrypted in worker)."
                    : "Resuming Income Tax portal login with user-submitted OTP.",
                cancellationToken);

            IReadOnlyList<PortalNoticeDto> portalNotices;
            try
            {
                portalNotices = await FetchNoticesWithRetryAsync(
                    syncJobId,
                    new PortalLoginRequest(
                        client.Credential.Username,
                        password,
                        client.Pan,
                        client.Name),
                    otp,
                    cancellationToken);
            }
            catch (PortalOtpRequiredException)
            {
                await MarkAwaitingOtpAsync(syncJobId, cancellationToken);
                return;
            }

            await AddLogAsync(syncJobId, "Info", $"Portal returned {portalNotices.Count} notice(s).", cancellationToken);

            var upserted = await UpsertNoticesAsync(job.OrganizationId, client.Id, portalNotices, cancellationToken);

            var now = DateTimeOffset.UtcNow;
            var nextSync = ClientService.ComputeNextSync(now, client.SyncFrequency);

            await db.Clients
                .Where(c => c.Id == client.Id)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(c => c.LastSyncAtUtc, now)
                        .SetProperty(c => c.NextSyncAtUtc, nextSync),
                    cancellationToken);

            await db.SyncJobs
                .Where(j => j.Id == syncJobId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(j => j.Status, SyncJobStatus.Succeeded)
                        .SetProperty(j => j.CompletedAtUtc, now)
                        .SetProperty(j => j.NoticesUpserted, upserted)
                        .SetProperty(j => j.ErrorMessage, (string?)null)
                        .SetProperty(j => j.SubmittedOtpProtected, (string?)null),
                    cancellationToken);

            await usageLimitsService.ConsumeSyncCreditAsync(job.OrganizationId, syncJobId, cancellationToken);

            await AddLogAsync(syncJobId, "Info",
                $"Sync succeeded. Upserted {upserted} notice(s). Next sync at {nextSync:u}.",
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Sync job {JobId} failed ({ExceptionType})", syncJobId, ex.GetType().Name);
            await MarkFailedAsync(syncJobId, MapPortalFailureMessage(ex), cancellationToken);
        }
    }

    private async Task<IReadOnlyList<PortalNoticeDto>> FetchNoticesWithRetryAsync(
        Guid syncJobId,
        PortalLoginRequest request,
        string? otp,
        CancellationToken cancellationToken)
    {
        Exception? lastTransient = null;

        for (var attempt = 1; attempt <= PortalCallDefaults.MaxAttempts; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(PortalCallDefaults.CallTimeout);

            try
            {
                return await portalClient.FetchNoticesAsync(request, otp, timeoutCts.Token);
            }
            catch (PortalOtpRequiredException)
            {
                throw;
            }
            catch (PortalAuthException)
            {
                throw;
            }
            catch (Exception ex) when (IsTransientPortalFailure(ex) && attempt < PortalCallDefaults.MaxAttempts)
            {
                lastTransient = ex;
                logger.LogWarning(
                    "Portal call attempt {Attempt}/{MaxAttempts} failed transiently for sync job {JobId}",
                    attempt,
                    PortalCallDefaults.MaxAttempts,
                    syncJobId);
                await AddLogAsync(
                    syncJobId,
                    "Warn",
                    $"Portal temporarily unavailable (attempt {attempt}/{PortalCallDefaults.MaxAttempts}). Retrying…",
                    cancellationToken);
                await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken);
            }
            catch (Exception ex) when (IsTransientPortalFailure(ex))
            {
                throw new PortalTransientException(
                    "Income Tax portal timed out or is temporarily unavailable. Try sync again.",
                    ex);
            }
        }

        throw lastTransient
            ?? new PortalTransientException("Income Tax portal timed out or is temporarily unavailable. Try sync again.");
    }

    private static bool IsTransientPortalFailure(Exception ex) =>
        ex is PortalTransientException
            or OperationCanceledException
            or TimeoutException
            or TaskCanceledException;

    /// <summary>Maps exceptions to user-facing text that never includes secrets.</summary>
    internal static string MapPortalFailureMessage(Exception ex) =>
        ex switch
        {
            PortalAuthException auth => auth.Message,
            PortalTransientException transient => transient.Message,
            OperationCanceledException or TimeoutException or TaskCanceledException =>
                "Income Tax portal timed out or is temporarily unavailable. Try sync again.",
            _ => "Income Tax portal sync failed. Try again or check portal credentials."
        };

    private async Task FailTimedOutOtpJobsAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow - OtpTimeout;
        var timedOutIds = await db.SyncJobs
            .AsNoTracking()
            .Where(j => j.Status == SyncJobStatus.AwaitingOtp
                        && j.OtpRequestedAtUtc != null
                        && j.OtpRequestedAtUtc < cutoff)
            .Select(j => j.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in timedOutIds)
        {
            await MarkFailedAsync(
                id,
                $"Vault OTP was not submitted within {OtpTimeout.TotalMinutes:0} minutes.",
                cancellationToken);
        }
    }

    private async Task<int> UpsertNoticesAsync(
        Guid organizationId,
        Guid clientId,
        IReadOnlyList<PortalNoticeDto> portalNotices,
        CancellationToken cancellationToken)
    {
        var upserted = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var item in portalNotices)
        {
            if (string.IsNullOrWhiteSpace(item.DocumentReferenceId))
            {
                continue;
            }

            var kind = Enum.TryParse<NoticeKind>(item.Kind, ignoreCase: true, out var parsedKind)
                ? parsedKind
                : NoticeKind.Notice;

            var docRef = TruncateRequired(item.DocumentReferenceId, 64);
            var existing = await db.Notices
                .FirstOrDefaultAsync(
                    n => n.OrganizationId == organizationId
                         && n.ClientId == clientId
                         && n.DocumentReferenceId == docRef,
                    cancellationToken);

            if (existing is null)
            {
                db.Notices.Add(new Notice
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    ClientId = clientId,
                    Module = ComplianceModule.IncomeTax,
                    Kind = kind,
                    Section = TruncateRequired(item.Section, 64),
                    Description = TruncateRequired(item.Description, 500),
                    FinancialYear = TruncateOptional(item.FinancialYear, 16),
                    ProceedingId = TruncateOptional(item.ProceedingId, 64),
                    DocumentReferenceId = docRef,
                    Status = NoticeWorkflowStatus.New,
                    ServedDate = item.ServedDate,
                    ResponseDueDate = item.ResponseDueDate,
                    CreatedAtUtc = now
                });
            }
            else
            {
                existing.Section = TruncateRequired(item.Section, 64);
                existing.Description = TruncateRequired(item.Description, 500);
                existing.FinancialYear = TruncateOptional(item.FinancialYear, 16);
                existing.ProceedingId = TruncateOptional(item.ProceedingId, 64);
                existing.Kind = kind;
                existing.ServedDate = item.ServedDate;
                existing.ResponseDueDate = item.ResponseDueDate;
            }

            upserted++;
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }

        return upserted;
    }

    private async Task MarkRunningAsync(
        Guid syncJobId,
        SyncJobStatus previousStatus,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await db.SyncJobs
            .Where(j => j.Id == syncJobId
                        && (j.Status == SyncJobStatus.Pending || j.Status == SyncJobStatus.AwaitingOtp))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(j => j.Status, SyncJobStatus.Running)
                    .SetProperty(j => j.StartedAtUtc, j => j.StartedAtUtc ?? now)
                    .SetProperty(j => j.SubmittedOtpProtected, (string?)null)
                    .SetProperty(j => j.ErrorMessage, (string?)null),
                cancellationToken);

        var message = previousStatus == SyncJobStatus.AwaitingOtp
            ? "Sync resumed after OTP submission."
            : "Sync started (portal login).";
        await AddLogAsync(syncJobId, "Info", message, cancellationToken);
    }

    private async Task MarkAwaitingOtpAsync(Guid syncJobId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await db.SyncJobs
            .Where(j => j.Id == syncJobId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(j => j.Status, SyncJobStatus.AwaitingOtp)
                    .SetProperty(j => j.OtpRequestedAtUtc, now)
                    .SetProperty(j => j.SubmittedOtpProtected, (string?)null)
                    .SetProperty(j => j.ErrorMessage, (string?)null),
                cancellationToken);

        await AddLogAsync(
            syncJobId,
            "Info",
            "Portal requires vault OTP. Waiting for user to submit the one-time password.",
            cancellationToken);
    }

    private async Task MarkFailedAsync(Guid syncJobId, string message, CancellationToken cancellationToken)
    {
        var truncated = TruncateRequired(message, 1000);
        var now = DateTimeOffset.UtcNow;
        await db.SyncJobs
            .Where(j => j.Id == syncJobId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(j => j.Status, SyncJobStatus.Failed)
                    .SetProperty(j => j.CompletedAtUtc, now)
                    .SetProperty(j => j.ErrorMessage, truncated)
                    .SetProperty(j => j.SubmittedOtpProtected, (string?)null),
                cancellationToken);

        await AddLogAsync(syncJobId, "Error", truncated, cancellationToken);
    }

    private async Task AddLogAsync(
        Guid syncJobId,
        string level,
        string message,
        CancellationToken cancellationToken)
    {
        db.SyncJobLogs.Add(new SyncJobLog
        {
            Id = Guid.NewGuid(),
            SyncJobId = syncJobId,
            AtUtc = DateTimeOffset.UtcNow,
            Level = level,
            Message = TruncateRequired(message, 1000)
        });
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    private static string TruncateRequired(string? value, int max)
    {
        var text = value ?? string.Empty;
        return text.Length <= max ? text : text[..max];
    }

    private static string? TruncateOptional(string? value, int max) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Length <= max ? value : value[..max];
}
