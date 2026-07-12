using NoticeSaaS.Domain.Enums;

namespace NoticeSaaS.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public CompanyType CompanyType { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
}
