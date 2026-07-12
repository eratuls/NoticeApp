using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoticeSaaS.Domain.Entities;
using NoticeSaaS.Domain.Enums;

namespace NoticeSaaS.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public const string SeedAdminEmail = "admin@noticesaas.local";
    public const string SeedAdminPassword = "Admin@12345";
    public const string SeedOrganizationName = "NoticeSaaS Demo";

    private static readonly Guid SeedOrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SeedAdminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SeedOwnerRoleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SeedAdminRoleId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SeedManagerRoleId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid SeedStaffRoleId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid SeedClientViewerRoleId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid SeedMembershipId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static async Task SeedAsync(
        NoticeSaaSDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(db, cancellationToken);
        await SeedAdminWorkspaceAsync(db, logger, cancellationToken);
    }

    private static async Task SeedRolesAsync(NoticeSaaSDbContext db, CancellationToken cancellationToken)
    {
        var roles = new[]
        {
            new Role { Id = SeedOwnerRoleId, Name = SystemRoles.Owner, Description = "Organization owner", IsSystem = true },
            new Role { Id = SeedAdminRoleId, Name = SystemRoles.Admin, Description = "Organization admin", IsSystem = true },
            new Role { Id = SeedManagerRoleId, Name = SystemRoles.Manager, Description = "Team manager", IsSystem = true },
            new Role { Id = SeedStaffRoleId, Name = SystemRoles.Staff, Description = "Staff member", IsSystem = true },
            new Role { Id = SeedClientViewerRoleId, Name = SystemRoles.ClientViewer, Description = "Client viewer", IsSystem = true }
        };

        foreach (var role in roles)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == role.Name, cancellationToken))
            {
                db.Roles.Add(role);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAdminWorkspaceAsync(
        NoticeSaaSDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(u => u.Email == SeedAdminEmail, cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var hasher = new PasswordHasher<User>();
        var admin = new User
        {
            Id = SeedAdminUserId,
            Email = SeedAdminEmail,
            FirstName = "System",
            LastName = "Admin",
            PasswordHash = string.Empty,
            IsActive = true,
            CreatedAtUtc = now
        };
        admin.PasswordHash = hasher.HashPassword(admin, SeedAdminPassword);

        var organization = new Organization
        {
            Id = SeedOrganizationId,
            Name = SeedOrganizationName,
            CompanyType = CompanyType.Ca,
            CreatedAtUtc = now
        };

        var ownerRole = await db.Roles.SingleAsync(r => r.Name == SystemRoles.Owner, cancellationToken);

        db.Organizations.Add(organization);
        db.Users.Add(admin);
        db.OrganizationMembers.Add(new OrganizationMember
        {
            Id = SeedMembershipId,
            OrganizationId = organization.Id,
            UserId = admin.Id,
            RoleId = ownerRole.Id,
            CreatedAtUtc = now
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Seeded admin workspace {Organization} with user {Email}",
            SeedOrganizationName,
            SeedAdminEmail);
    }
}
