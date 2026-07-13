using NoticeSaaS.Domain.Enums;

namespace NoticeSaaS.Domain.Entities;

public class Client
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public required string Name { get; set; }

    public required string Pan { get; set; }

    public string? CaPan { get; set; }

    public ComplianceModule Module { get; set; } = ComplianceModule.IncomeTax;

    public SyncFrequency SyncFrequency { get; set; } = SyncFrequency.Weekly;

    public string? PortalUsername { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? LastSyncAtUtc { get; set; }

    public DateTimeOffset? NextSyncAtUtc { get; set; }

    public PortalCredential? Credential { get; set; }

    public ICollection<Notice> Notices { get; set; } = new List<Notice>();
}
