namespace NoticeSaaS.Domain.Entities;

public class NoticeComment
{
    public Guid Id { get; set; }

    public Guid NoticeId { get; set; }

    public Notice Notice { get; set; } = null!;

    public Guid AuthorUserId { get; set; }

    public User Author { get; set; } = null!;

    public required string Body { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
