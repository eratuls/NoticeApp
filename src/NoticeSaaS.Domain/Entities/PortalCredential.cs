namespace NoticeSaaS.Domain.Entities;

public class PortalCredential
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;

    public required string Username { get; set; }

    /// <summary>Data-Protection protected portal password (never store plaintext).</summary>
    public required string PasswordProtected { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
