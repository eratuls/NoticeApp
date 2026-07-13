namespace NoticeSaaS.Domain.Entities;

public class SyncCreditLedgerEntry
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public Guid? SubscriptionId { get; set; }

    public OrganizationSubscription? Subscription { get; set; }

    public Guid? SyncJobId { get; set; }

    public SyncJob? SyncJob { get; set; }

    /// <summary>Negative when consuming credits; positive when granting.</summary>
    public int Delta { get; set; }

    public int BalanceAfter { get; set; }

    public required string Reason { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
