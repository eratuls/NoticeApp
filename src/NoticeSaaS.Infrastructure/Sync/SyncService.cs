using Microsoft.EntityFrameworkCore;
using NoticeSaaS.Application.Sync;
using NoticeSaaS.Domain.Entities;
using NoticeSaaS.Domain.Enums;
using NoticeSaaS.Infrastructure.Persistence;

namespace NoticeSaaS.Infrastructure.Sync;

public sealed class SyncService(
    NoticeSaaSDbContext db,
    ISyncJobProcessor processor) : ISyncService
{
    public async Task<TriggerSyncResult> TriggerManualAsync(
        Guid organizationId,
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var client = await db.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == clientId && c.OrganizationId == organizationId,
                cancellationToken);

        if (client is null)
        {
            return TriggerSyncResult.Fail("Client not found.");
        }

        if (!client.IsActive)
        {
            return TriggerSyncResult.Fail("Client is inactive.");
        }

        var hasCredential = await db.PortalCredentials.AnyAsync(c => c.ClientId == clientId, cancellationToken);
        if (!hasCredential)
        {
            return TriggerSyncResult.Fail("Portal credentials are required before sync.");
        }

        var activeJobs = await db.SyncJobs
            .Where(j => j.ClientId == clientId
                        && (j.Status == SyncJobStatus.Pending || j.Status == SyncJobStatus.Running))
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var active in activeJobs)
        {
            var age = now - (active.StartedAtUtc ?? active.CreatedAtUtc);
            if (age < TimeSpan.FromMinutes(2) && active.Status == SyncJobStatus.Running)
            {
                return TriggerSyncResult.Fail("A sync job is already in progress for this client.");
            }

            active.Status = SyncJobStatus.Failed;
            active.CompletedAtUtc = now;
            active.ErrorMessage = "Superseded by a newer sync request.";
        }

        var job = new SyncJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ClientId = clientId,
            Status = SyncJobStatus.Pending,
            Trigger = SyncJobTrigger.Manual,
            CreatedAtUtc = now
        };

        db.SyncJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);

        await processor.ProcessJobAsync(job.Id, cancellationToken);

        var dto = await GetJobDtoAsync(job.Id, cancellationToken);
        return dto is null
            ? TriggerSyncResult.Fail("Sync job could not be loaded after processing.")
            : TriggerSyncResult.Ok(dto);
    }

    public async Task<SyncJobDto?> GetLatestForClientAsync(
        Guid organizationId,
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var jobId = await db.SyncJobs.AsNoTracking()
            .Where(j => j.OrganizationId == organizationId && j.ClientId == clientId)
            .OrderByDescending(j => j.CreatedAtUtc)
            .Select(j => (Guid?)j.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return jobId is null ? null : await GetJobDtoAsync(jobId.Value, cancellationToken);
    }

    public async Task<int> EnqueueDueScheduledAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var dueClientIds = await db.Clients
            .Where(c => c.IsActive
                        && c.NextSyncAtUtc != null
                        && c.NextSyncAtUtc <= now
                        && db.PortalCredentials.Any(p => p.ClientId == c.Id))
            .Where(c => !db.SyncJobs.Any(j =>
                j.ClientId == c.Id
                && (j.Status == SyncJobStatus.Pending || j.Status == SyncJobStatus.Running)))
            .Select(c => new { c.Id, c.OrganizationId })
            .Take(20)
            .ToListAsync(cancellationToken);

        var count = 0;
        foreach (var client in dueClientIds)
        {
            db.SyncJobs.Add(new SyncJob
            {
                Id = Guid.NewGuid(),
                OrganizationId = client.OrganizationId,
                ClientId = client.Id,
                Status = SyncJobStatus.Pending,
                Trigger = SyncJobTrigger.Schedule,
                CreatedAtUtc = now
            });
            count++;
        }

        if (count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return count;
    }

    private async Task<SyncJobDto?> GetJobDtoAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await db.SyncJobs.AsNoTracking()
            .Include(j => j.Logs)
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job is null)
        {
            return null;
        }

        return new SyncJobDto(
            job.Id,
            job.ClientId,
            job.Status.ToString(),
            job.Trigger.ToString(),
            job.CreatedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            job.ErrorMessage,
            job.NoticesUpserted,
            job.Logs
                .OrderBy(l => l.AtUtc)
                .Select(l => new SyncJobLogDto(l.AtUtc, l.Level, l.Message))
                .ToList());
    }
}
