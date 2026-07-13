using Microsoft.EntityFrameworkCore;
using NoticeSaaS.Domain.Entities;

namespace NoticeSaaS.Infrastructure.Persistence;

public class NoticeSaaSDbContext(DbContextOptions<NoticeSaaSDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Notice> Notices => Set<Notice>();
    public DbSet<PortalCredential> PortalCredentials => Set<PortalCredential>();
    public DbSet<NoticeComment> NoticeComments => Set<NoticeComment>();
    public DbSet<NoticeStatusEvent> NoticeStatusEvents => Set<NoticeStatusEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NoticeSaaSDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
