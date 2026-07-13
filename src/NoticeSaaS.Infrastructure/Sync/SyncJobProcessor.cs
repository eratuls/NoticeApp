using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    ILogger<SyncJobProcessor> logger) : ISyncJobProcessor
{
    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector("NoticeSaaS.PortalCredentials.v1");

    public async Task ProcessPendingAsync(int maxJobs, CancellationToken cancellationToken = default)
    {
        var jobIds = await db.SyncJobs
            .AsNoTracking()
            .Where(j => j.Status == SyncJobStatus.Pending)
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

        if (job is null || job.Status is SyncJobStatus.Succeeded or SyncJobStatus.Failed or SyncJobStatus.Running)
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

        await MarkRunningAsync(syncJobId, cancellationToken);

        try
        {
            string password;
            try
            {
                password = _protector.Unprotect(client.Credential.PasswordProtected);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to decrypt portal credentials for client {ClientId}", client.Id);
                await MarkFailedAsync(syncJobId, "Unable to decrypt portal credentials.", cancellationToken);
                return;
            }

            await AddLogAsync(syncJobId, "Info",
                $"Logging in as {client.Credential.Username} (credentials decrypted in worker).",
                cancellationToken);

            var portalNotices = await portalClient.FetchNoticesAsync(
                new PortalLoginRequest(
                    client.Credential.Username,
                    password,
                    client.Pan,
                    client.Name),
                cancellationToken);

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
                        .SetProperty(j => j.ErrorMessage, (string?)null),
                    cancellationToken);

            await AddLogAsync(syncJobId, "Info",
                $"Sync succeeded. Upserted {upserted} notice(s). Next sync at {nextSync:u}.",
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Sync job {JobId} failed", syncJobId);
            await MarkFailedAsync(syncJobId, ex.Message, cancellationToken);
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

    private async Task MarkRunningAsync(Guid syncJobId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await db.SyncJobs
            .Where(j => j.Id == syncJobId && j.Status == SyncJobStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(j => j.Status, SyncJobStatus.Running)
                    .SetProperty(j => j.StartedAtUtc, now),
                cancellationToken);

        await AddLogAsync(syncJobId, "Info", "Sync started (password-only portal path).", cancellationToken);
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
                    .SetProperty(j => j.ErrorMessage, truncated),
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
