using NoticeSaaS.Domain.Enums;

namespace NoticeSaaS.Domain.Entities;

public class Reminder
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public Guid? NoticeId { get; set; }

    public Notice? Notice { get; set; }

    public Guid? ClientId { get; set; }

    public Client? Client { get; set; }

    public ComplianceModule Module { get; set; } = ComplianceModule.IncomeTax;

    public required string Description { get; set; }

    public string? ProceedingId { get; set; }

    public string? DocumentReferenceId { get; set; }

    public string? AssesseeIdentifier { get; set; }

    public ReminderPriority Priority { get; set; } = ReminderPriority.Medium;

    public DateOnly DueOn { get; set; }

    public bool IsDone { get; set; }

    public Guid CreatedByUserId { get; set; }

    public User CreatedBy { get; set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }
}
