namespace NoticeSaaS.Domain.Entities;

public class OrganizationSubscription
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public required string PlanName { get; set; }

    public bool IsActive { get; set; } = true;

    public int AssesseeLimit { get; set; }

    public int SyncCreditLimit { get; set; }

    public int SyncCreditsUsed { get; set; }

    public DateTimeOffset StartsAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>Comma-separated module names enabled on the plan.</summary>
    public required string ModulesEnabled { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<SyncCreditLedgerEntry> CreditLedger { get; set; } = new List<SyncCreditLedgerEntry>();
}
