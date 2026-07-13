using NoticeSaaS.Domain.Enums;

namespace NoticeSaaS.Domain.Entities;

public class SyncJob
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;

    public SyncJobStatus Status { get; set; } = SyncJobStatus.Pending;

    public SyncJobTrigger Trigger { get; set; } = SyncJobTrigger.Manual;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public string? ErrorMessage { get; set; }

    public int NoticesUpserted { get; set; }

    public ICollection<SyncJobLog> Logs { get; set; } = new List<SyncJobLog>();
}
