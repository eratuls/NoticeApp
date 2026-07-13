using NoticeSaaS.Domain.Enums;

namespace NoticeSaaS.Domain.Entities;

public class NoticeStatusEvent
{
    public Guid Id { get; set; }

    public Guid NoticeId { get; set; }

    public Notice Notice { get; set; } = null!;

    public NoticeWorkflowStatus? FromStatus { get; set; }

    public NoticeWorkflowStatus ToStatus { get; set; }

    public Guid? ChangedByUserId { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
