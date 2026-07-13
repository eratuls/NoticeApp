namespace NoticeSaaS.Domain.Entities;

public class AppNotification
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string Title { get; set; }

    public required string Body { get; set; }

    public bool IsRead { get; set; }

    public Guid? NoticeId { get; set; }

    public Guid? ReminderId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? ReadAtUtc { get; set; }
}
