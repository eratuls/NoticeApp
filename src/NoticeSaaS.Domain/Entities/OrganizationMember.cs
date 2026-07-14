namespace NoticeSaaS.Domain.Entities;

public class OrganizationMember
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public Guid? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public Guid? DesignationId { get; set; }

    public Designation? Designation { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
