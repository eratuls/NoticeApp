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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NoticeSaaSDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
