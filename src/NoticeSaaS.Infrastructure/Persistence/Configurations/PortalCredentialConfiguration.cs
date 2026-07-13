using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NoticeSaaS.Domain.Entities;

namespace NoticeSaaS.Infrastructure.Persistence.Configurations;

public class PortalCredentialConfiguration : IEntityTypeConfiguration<PortalCredential>
{
    public void Configure(EntityTypeBuilder<PortalCredential> builder)
    {
        builder.ToTable("PortalCredentials");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PasswordProtected).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.ClientId).IsUnique();
    }
}
