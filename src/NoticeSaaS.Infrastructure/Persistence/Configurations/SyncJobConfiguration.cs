using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoticeSaaS.Domain.Entities;

namespace NoticeSaaS.Infrastructure.Persistence.Configurations;

public class SyncJobConfiguration : IEntityTypeConfiguration<SyncJob>
{
    public void Configure(EntityTypeBuilder<SyncJob> builder)
    {
        builder.ToTable("SyncJobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Trigger).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
        builder.Property(x => x.SubmittedOtpProtected).HasMaxLength(500);

        builder.HasIndex(x => new { x.ClientId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.OrganizationId, x.ClientId, x.CreatedAtUtc });

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Client)
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
