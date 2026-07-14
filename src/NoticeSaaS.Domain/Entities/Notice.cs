using NoticeSaaS.Domain.Enums;

namespace NoticeSaaS.Domain.Entities;

public class Notice
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;

    public ComplianceModule Module { get; set; } = ComplianceModule.IncomeTax;

    public NoticeKind Kind { get; set; } = NoticeKind.Notice;

    public required string Section { get; set; }

    public required string Description { get; set; }

    public string? FinancialYear { get; set; }

    public string? ProceedingId { get; set; }

    public string? DocumentReferenceId { get; set; }

    public NoticeWorkflowStatus Status { get; set; } = NoticeWorkflowStatus.New;

    public DateOnly? ServedDate { get; set; }

    public DateOnly? ResponseDueDate { get; set; }

    public DateOnly? ResponseSubmittedDate { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? ClosedAtUtc { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public User? AssignedTo { get; set; }

    public ICollection<NoticeComment> Comments { get; set; } = new List<NoticeComment>();

    public ICollection<NoticeStatusEvent> StatusEvents { get; set; } = new List<NoticeStatusEvent>();

    public ICollection<NoticeAttachment> Attachments { get; set; } = new List<NoticeAttachment>();

    public bool IsOverdue(DateOnly today) =>
        Status != NoticeWorkflowStatus.Closed
        && ResponseDueDate is not null
        && ResponseDueDate.Value < today;
}
