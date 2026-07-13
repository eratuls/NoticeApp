using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoticeSaaS.Domain.Entities;

namespace NoticeSaaS.Infrastructure.Persistence.Configurations;

public class SyncCreditLedgerEntryConfiguration : IEntityTypeConfiguration<SyncCreditLedgerEntry>
{
    public void Configure(EntityTypeBuilder<SyncCreditLedgerEntry> builder)
    {
        builder.ToTable("SyncCreditLedger");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.OrganizationId, x.CreatedAtUtc });

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Subscription)
            .WithMany(x => x.CreditLedger)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.SyncJob)
            .WithMany()
            .HasForeignKey(x => x.SyncJobId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
