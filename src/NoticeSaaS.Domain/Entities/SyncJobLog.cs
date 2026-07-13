namespace NoticeSaaS.Domain.Entities;

public class SyncJobLog
{
    public Guid Id { get; set; }

    public Guid SyncJobId { get; set; }

    public SyncJob SyncJob { get; set; } = null!;

    public DateTimeOffset AtUtc { get; set; }

    public required string Level { get; set; }

    public required string Message { get; set; }
}
