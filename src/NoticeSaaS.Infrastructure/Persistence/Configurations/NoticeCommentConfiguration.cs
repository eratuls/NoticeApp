using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoticeSaaS.Domain.Entities;

namespace NoticeSaaS.Infrastructure.Persistence.Configurations;

public class NoticeCommentConfiguration : IEntityTypeConfiguration<NoticeComment>
{
    public void Configure(EntityTypeBuilder<NoticeComment> builder)
    {
        builder.ToTable("NoticeComments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();

        builder.HasOne(x => x.Notice)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.NoticeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
