using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoticeSaaS.Domain.Entities;

namespace NoticeSaaS.Infrastructure.Persistence.Configurations;

public class NoticeStatusEventConfiguration : IEntityTypeConfiguration<NoticeStatusEvent>
{
    public void Configure(EntityTypeBuilder<NoticeStatusEvent> builder)
    {
        builder.ToTable("NoticeStatusEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.Notice)
            .WithMany(x => x.StatusEvents)
            .HasForeignKey(x => x.NoticeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
