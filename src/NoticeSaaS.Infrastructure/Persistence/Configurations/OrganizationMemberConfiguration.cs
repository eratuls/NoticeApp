using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoticeSaaS.Domain.Entities;

namespace NoticeSaaS.Infrastructure.Persistence.Configurations;

public class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.ToTable("OrganizationMembers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();

        builder.HasOne(x => x.Organization)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Memberships)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Role)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Department)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Designation)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.DesignationId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
