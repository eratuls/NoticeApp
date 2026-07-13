using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoticeSaaS.Domain.Entities;

namespace NoticeSaaS.Infrastructure.Persistence.Configurations;

public class OrganizationSubscriptionConfiguration : IEntityTypeConfiguration<OrganizationSubscription>
{
    public void Configure(EntityTypeBuilder<OrganizationSubscription> builder)
    {
        builder.ToTable("OrganizationSubscriptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PlanName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ModulesEnabled).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.OrganizationId).IsUnique();

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
