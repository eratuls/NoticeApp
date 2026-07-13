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
    private static readonly Guid SeedClientId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public static async Task SeedAsync(
        NoticeSaaSDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(db, cancellationToken);
        await SeedAdminWorkspaceAsync(db, logger, cancellationToken);
        await SeedDemoClientsAndNoticesAsync(db, logger, cancellationToken);
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

    private static async Task SeedDemoClientsAndNoticesAsync(
        NoticeSaaSDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.Clients.AnyAsync(c => c.Id == SeedClientId, cancellationToken))
        {
            return;
        }

        if (!await db.Organizations.AnyAsync(o => o.Id == SeedOrganizationId, cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var client = new Client
        {
            Id = SeedClientId,
            OrganizationId = SeedOrganizationId,
            Name = "Marshal Quarries And Granites Pvt Ltd",
            Pan = "AABCM1234F",
            Module = ComplianceModule.IncomeTax,
            IsActive = true,
            CreatedAtUtc = now.AddDays(-12)
        };

        var notices = new List<Notice>
        {
            MakeNotice(1, "143(2)", "Scrutiny notice — books of account", NoticeWorkflowStatus.New, today.AddDays(-2), today.AddDays(12)),
            MakeNotice(2, "142(1)", "Inquiry before assessment", NoticeWorkflowStatus.New, today.AddDays(-5), today.AddDays(8)),
            MakeNotice(3, "133(6)", "Information call", NoticeWorkflowStatus.New, today.AddDays(-1), today.AddDays(20)),
            MakeNotice(4, "148", "Income escaping assessment", NoticeWorkflowStatus.Open, today.AddDays(-20), today.AddDays(5)),
            MakeNotice(5, "156", "Demand notice follow-up", NoticeWorkflowStatus.InProgress, today.AddDays(-30), today.AddDays(-3)),
            MakeNotice(6, "154", "Rectification pending reply", NoticeWorkflowStatus.Replied, today.AddDays(-40), today.AddDays(-10)),
            MakeNotice(7, "245", "Stay petition outcome", NoticeWorkflowStatus.Closed, today.AddDays(-90), today.AddDays(-60), now.AddDays(-55)),
            MakeNotice(8, "220(2)", "Interest demand closed", NoticeWorkflowStatus.Closed, today.AddDays(-120), today.AddDays(-100), now.AddDays(-95)),
            MakeNotice(9, "271(1)(c)", "Penalty proceedings closed", NoticeWorkflowStatus.Closed, today.AddDays(-150), today.AddDays(-130), now.AddDays(-125)),
            MakeNotice(10, "139(9)", "Defective return closed", NoticeWorkflowStatus.Closed, today.AddDays(-80), today.AddDays(-70), now.AddDays(-68)),
            MakeNotice(11, "143(1)", "Intimation closed", NoticeWorkflowStatus.Closed, today.AddDays(-200), today.AddDays(-190), now.AddDays(-185)),
            MakeNotice(12, "200A", "TDS statement closed", NoticeWorkflowStatus.Closed, today.AddDays(-110), today.AddDays(-100), now.AddDays(-98)),
            MakeNotice(13, "206C", "TCS mismatch closed", NoticeWorkflowStatus.Closed, today.AddDays(-95), today.AddDays(-85), now.AddDays(-82)),
            MakeNotice(14, "234E", "Late fee closed", NoticeWorkflowStatus.Closed, today.AddDays(-70), today.AddDays(-60), now.AddDays(-58)),
            MakeNotice(15, "119", "Condensation closed", NoticeWorkflowStatus.Closed, today.AddDays(-60), today.AddDays(-50), now.AddDays(-48)),
            MakeNotice(16, "131", "Summon response closed", NoticeWorkflowStatus.Closed, today.AddDays(-55), today.AddDays(-45), now.AddDays(-44)),
            MakeNotice(17, "133A", "Survey follow-up closed", NoticeWorkflowStatus.Closed, today.AddDays(-48), today.AddDays(-40), now.AddDays(-38)),
            MakeNotice(18, "142(2)", "Special audit closed", NoticeWorkflowStatus.Closed, today.AddDays(-44), today.AddDays(-35), now.AddDays(-33)),
            MakeNotice(19, "147", "Reassessment closed", NoticeWorkflowStatus.Closed, today.AddDays(-42), today.AddDays(-32), now.AddDays(-30)),
            MakeNotice(20, "246A", "Appeal filed closed", NoticeWorkflowStatus.Closed, today.AddDays(-38), today.AddDays(-28), now.AddDays(-26)),
            MakeNotice(21, "250", "CIT(A) order closed", NoticeWorkflowStatus.Closed, today.AddDays(-36), today.AddDays(-26), now.AddDays(-24))
        };

        db.Clients.Add(client);
        db.Notices.AddRange(notices);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Seeded demo client {Pan} with {NoticeCount} Income Tax notices",
                client.Pan,
                notices.Count);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
        }

        static Notice MakeNotice(
            int index,
            string section,
            string description,
            NoticeWorkflowStatus status,
            DateOnly served,
            DateOnly due,
            DateTimeOffset? closedAt = null) =>
            new()
            {
                Id = Guid.Parse($"55555555-5555-5555-5555-{index:D12}"),
                OrganizationId = SeedOrganizationId,
                ClientId = SeedClientId,
                Module = ComplianceModule.IncomeTax,
                Section = section,
                Description = description,
                FinancialYear = "2024-25",
                ProceedingId = $"PROC-{index:D4}",
                DocumentReferenceId = $"DIN-{2024000 + index}",
                Status = status,
                ServedDate = served,
                ResponseDueDate = due,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-index),
                ClosedAtUtc = closedAt
            };
    }
}
