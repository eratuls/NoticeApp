using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoticeSaaS.Domain.Entities;

namespace NoticeSaaS.Infrastructure.Persistence.Configurations;

public class SyncJobLogConfiguration : IEntityTypeConfiguration<SyncJobLog>
{
    public void Configure(EntityTypeBuilder<SyncJobLog> builder)
    {
        builder.ToTable("SyncJobLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Level).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();

        builder.HasIndex(x => new { x.SyncJobId, x.AtUtc });

        builder.HasOne(x => x.SyncJob)
            .WithMany(x => x.Logs)
            .HasForeignKey(x => x.SyncJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
