using Microsoft.AspNetCore.DataProtection;
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
        IDataProtector? portalCredentialProtector = null,
        CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(db, cancellationToken);
        await SeedAdminWorkspaceAsync(db, logger, cancellationToken);
        await SeedDemoClientsAndNoticesAsync(db, logger, portalCredentialProtector, cancellationToken);
    }

    private static async Task SeedRolesAsync(NoticeSaaSDbContext db, CancellationToken cancellationToken)
    {
        var desired = new[]
        {
            new Role { Id = SeedOwnerRoleId, Name = SystemRoles.Owner, Description = "Organization owner", IsSystem = true },
            new Role { Id = SeedAdminRoleId, Name = SystemRoles.Admin, Description = "Organization admin", IsSystem = true },
            new Role { Id = SeedManagerRoleId, Name = SystemRoles.Manager, Description = "Team manager", IsSystem = true },
            new Role { Id = SeedStaffRoleId, Name = SystemRoles.Staff, Description = "Staff member", IsSystem = true },
            new Role { Id = SeedClientViewerRoleId, Name = SystemRoles.ClientViewer, Description = "Client viewer", IsSystem = true }
        };

        foreach (var role in desired)
        {
            var existing = await db.Roles.FirstOrDefaultAsync(r => r.Id == role.Id || r.Name == role.Name, cancellationToken);
            if (existing is null)
            {
                db.Roles.Add(role);
                continue;
            }

            existing.Name = role.Name;
            existing.Description = role.Description;
            existing.IsSystem = true;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAdminWorkspaceAsync(
        NoticeSaaSDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var hasher = new PasswordHasher<User>();

        var organization = await db.Organizations.FirstOrDefaultAsync(o => o.Id == SeedOrganizationId, cancellationToken);
        if (organization is null)
        {
            organization = new Organization
            {
                Id = SeedOrganizationId,
                Name = SeedOrganizationName,
                CompanyType = CompanyType.Ca,
                CreatedAtUtc = now
            };
            db.Organizations.Add(organization);
        }
        else
        {
            organization.Name = SeedOrganizationName;
            organization.CompanyType = CompanyType.Ca;
        }

        var admin = await db.Users.FirstOrDefaultAsync(
            u => u.Id == SeedAdminUserId || u.Email == SeedAdminEmail,
            cancellationToken);
        if (admin is null)
        {
            admin = new User
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
            db.Users.Add(admin);
        }
        else
        {
            admin.Email = SeedAdminEmail;
            admin.FirstName = "System";
            admin.LastName = "Admin";
            admin.IsActive = true;
            admin.PasswordHash = hasher.HashPassword(admin, SeedAdminPassword);
        }

        var ownerRole = await db.Roles.SingleAsync(r => r.Name == SystemRoles.Owner, cancellationToken);
        var membership = await db.OrganizationMembers.FirstOrDefaultAsync(
            m => m.Id == SeedMembershipId || (m.OrganizationId == SeedOrganizationId && m.UserId == admin.Id),
            cancellationToken);
        if (membership is null)
        {
            db.OrganizationMembers.Add(new OrganizationMember
            {
                Id = SeedMembershipId,
                OrganizationId = SeedOrganizationId,
                UserId = admin.Id,
                RoleId = ownerRole.Id,
                CreatedAtUtc = now
            });
        }
        else
        {
            membership.OrganizationId = SeedOrganizationId;
            membership.UserId = admin.Id;
            membership.RoleId = ownerRole.Id;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Ensured demo admin workspace {Organization} / {Email}",
            SeedOrganizationName,
            SeedAdminEmail);
    }

    private static async Task SeedDemoClientsAndNoticesAsync(
        NoticeSaaSDbContext db,
        ILogger logger,
        IDataProtector? portalCredentialProtector,
        CancellationToken cancellationToken)
    {
        if (!await db.Organizations.AnyAsync(o => o.Id == SeedOrganizationId, cancellationToken))
        {
            logger.LogWarning("Skipping demo client/notice seed; organization {OrganizationId} is missing.", SeedOrganizationId);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        const string portalUser = "AABCM1234F";
        const SyncFrequency syncFrequency = SyncFrequency.Daily;

        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == SeedClientId, cancellationToken);
        if (client is null)
        {
            client = new Client
            {
                Id = SeedClientId,
                OrganizationId = SeedOrganizationId,
                Name = "Marshal Quarries And Granites Pvt Ltd",
                Pan = "AABCM1234F",
                Module = ComplianceModule.IncomeTax,
                SyncFrequency = syncFrequency,
                PortalUsername = portalUser,
                IsActive = true,
                CreatedAtUtc = now.AddDays(-12),
                LastSyncAtUtc = now.AddDays(-1),
                NextSyncAtUtc = now.AddDays(1)
            };
            db.Clients.Add(client);
        }
        else
        {
            client.OrganizationId = SeedOrganizationId;
            client.Name = "Marshal Quarries And Granites Pvt Ltd";
            client.Pan = "AABCM1234F";
            client.Module = ComplianceModule.IncomeTax;
            client.SyncFrequency = syncFrequency;
            client.PortalUsername = portalUser;
            client.IsActive = true;
            client.LastSyncAtUtc ??= now.AddDays(-1);
            client.NextSyncAtUtc ??= now.AddDays(1);
        }

        if (portalCredentialProtector is not null)
        {
            var seedCredentialId = Guid.Parse("66666666-6666-6666-6666-666666666666");
            var credential = await db.PortalCredentials.FirstOrDefaultAsync(
                c => c.Id == seedCredentialId || c.ClientId == SeedClientId,
                cancellationToken);
            if (credential is null)
            {
                db.PortalCredentials.Add(new PortalCredential
                {
                    Id = seedCredentialId,
                    ClientId = SeedClientId,
                    Username = portalUser,
                    PasswordProtected = portalCredentialProtector.Protect("DemoPortal@123"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
            }
            else
            {
                credential.Username = portalUser;
                credential.PasswordProtected = portalCredentialProtector.Protect("DemoPortal@123");
                credential.UpdatedAtUtc = now;
            }
        }

        var desiredNotices = BuildDemoNotices(today, now);
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE Notices SET Kind = N'Notice' WHERE Kind = N'' OR Kind IS NULL",
            cancellationToken);
        var desiredIds = desiredNotices.Select(n => n.Id).ToArray();
        var existingNotices = await db.Notices
            .Where(n => desiredIds.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, cancellationToken);

        foreach (var desired in desiredNotices)
        {
            if (!existingNotices.TryGetValue(desired.Id, out var existing))
            {
                db.Notices.Add(desired);
                continue;
            }

            existing.OrganizationId = desired.OrganizationId;
            existing.ClientId = desired.ClientId;
            existing.Module = desired.Module;
            existing.Kind = desired.Kind;
            existing.Section = desired.Section;
            existing.Description = desired.Description;
            existing.FinancialYear = desired.FinancialYear;
            existing.ProceedingId = desired.ProceedingId;
            existing.DocumentReferenceId = desired.DocumentReferenceId;
            existing.Status = desired.Status;
            existing.ServedDate = desired.ServedDate;
            existing.ResponseDueDate = desired.ResponseDueDate;
            existing.ClosedAtUtc = desired.ClosedAtUtc;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Ensured demo client {Pan} with {NoticeCount} Income Tax notices",
                client.Pan,
                desiredNotices.Count);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
        }
    }

    private static List<Notice> BuildDemoNotices(DateOnly today, DateTimeOffset now) =>
    [
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
        MakeNotice(21, "250", "CIT(A) order closed", NoticeWorkflowStatus.Closed, today.AddDays(-36), today.AddDays(-26), now.AddDays(-24)),
        MakeNotice(22, "156", "Outstanding demand order", NoticeWorkflowStatus.Open, today.AddDays(-15), today.AddDays(10), kind: NoticeKind.DirectOrder)
    ];

    private static Notice MakeNotice(
        int index,
        string section,
        string description,
        NoticeWorkflowStatus status,
        DateOnly served,
        DateOnly due,
        DateTimeOffset? closedAt = null,
        NoticeKind kind = NoticeKind.Notice) =>
        new()
        {
            Id = Guid.Parse($"55555555-5555-5555-5555-{index:D12}"),
            OrganizationId = SeedOrganizationId,
            ClientId = SeedClientId,
            Module = ComplianceModule.IncomeTax,
            Kind = kind,
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
