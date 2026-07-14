using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoticeSaaS.Domain.Entities;

namespace NoticeSaaS.Infrastructure.Persistence.Configurations;

public class NoticeAttachmentConfiguration : IEntityTypeConfiguration<NoticeAttachment>
{
    public void Configure(EntityTypeBuilder<NoticeAttachment> builder)
    {
        builder.ToTable("NoticeAttachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Category).HasMaxLength(32).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.StoredFileName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.NoticeId, x.Category });

        builder.HasOne(x => x.Notice)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.NoticeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.UploadedBy)
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
