namespace NoticeSaaS.Domain.Entities;

public class Role
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsSystem { get; set; }

    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
}
