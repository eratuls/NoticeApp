using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoticeSaaS.Domain.Entities;

namespace NoticeSaaS.Infrastructure.Persistence.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("Reminders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ProceedingId).HasMaxLength(64);
        builder.Property(x => x.DocumentReferenceId).HasMaxLength(64);
        builder.Property(x => x.AssesseeIdentifier).HasMaxLength(32);
        builder.Property(x => x.Module).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(x => new { x.OrganizationId, x.IsDone, x.DueOn });
        builder.HasIndex(x => new { x.OrganizationId, x.Priority });

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Notice)
            .WithMany()
            .HasForeignKey(x => x.NoticeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Client)
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
