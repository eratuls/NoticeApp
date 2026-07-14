namespace NoticeSaaS.Domain.Entities;

public class Department
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public required string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
}
