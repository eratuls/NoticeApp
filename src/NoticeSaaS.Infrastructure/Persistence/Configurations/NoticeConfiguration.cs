using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoticeSaaS.Domain.Entities;
using NoticeSaaS.Domain.Enums;

namespace NoticeSaaS.Infrastructure.Persistence.Configurations;

public class NoticeConfiguration : IEntityTypeConfiguration<Notice>
{
    public void Configure(EntityTypeBuilder<Notice> builder)
    {
        builder.ToTable("Notices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Section).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FinancialYear).HasMaxLength(16);
        builder.Property(x => x.ProceedingId).HasMaxLength(64);
        builder.Property(x => x.DocumentReferenceId).HasMaxLength(64);
        builder.Property(x => x.Module).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Kind)
            .HasConversion(
                v => v.ToString(),
                v => string.IsNullOrWhiteSpace(v) ? NoticeKind.Notice : Enum.Parse<NoticeKind>(v))
            .HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(x => new { x.OrganizationId, x.Status });
        builder.HasIndex(x => new { x.ClientId, x.Kind });
        builder.HasIndex(x => new { x.OrganizationId, x.ResponseDueDate });

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Client)
            .WithMany(x => x.Notices)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
