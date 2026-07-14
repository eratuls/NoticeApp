namespace NoticeSaaS.Domain.Entities;

public class NoticeAttachment
{
    public Guid Id { get; set; }

    public Guid NoticeId { get; set; }

    public Notice Notice { get; set; } = null!;

    /// <summary>NoticeDocument or Reply.</summary>
    public required string Category { get; set; }

    public required string FileName { get; set; }

    public required string ContentType { get; set; }

    public required string StoredFileName { get; set; }

    public long SizeBytes { get; set; }

    public Guid UploadedByUserId { get; set; }

    public User UploadedBy { get; set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
