using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NoticeSaaS.Application.Team;
using NoticeSaaS.Domain.Entities;
using NoticeSaaS.Domain.Enums;
using NoticeSaaS.Infrastructure.Persistence;

namespace NoticeSaaS.Infrastructure.Team;

public sealed class TeamService(NoticeSaaSDbContext db) : ITeamService
{
    private static readonly PasswordHasher<User> PasswordHasher = new();

    public async Task<TeamListResponse> ListAsync(
        Guid organizationId,
        string? role = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.OrganizationMembers.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId)
            .Include(m => m.User)
            .Include(m => m.Role)
            .Include(m => m.Department)
            .Include(m => m.Designation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleName = role.Trim();
            query = query.Where(m => m.Role.Name == roleName);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m =>
                m.User.FirstName.Contains(term)
                || m.User.LastName.Contains(term)
                || m.User.Email.Contains(term)
                || (m.User.PhoneNumber != null && m.User.PhoneNumber.Contains(term)));
        }

        var members = await query
            .OrderBy(m => m.User.FirstName)
            .ThenBy(m => m.User.LastName)
            .ToListAsync(cancellationToken);

        var items = members.Select(Map).ToList();
        var active = items.Count(m => m.IsActive);
        return new TeamListResponse(items.Count, active, items.Count - active, items);
    }

    public async Task<AddTeamMemberResult> AddAsync(
        Guid organizationId,
        AddTeamMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        var firstName = request.FirstName?.Trim() ?? string.Empty;
        var lastName = request.LastName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var phone = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();

        if (string.IsNullOrWhiteSpace(firstName)
            || string.IsNullOrWhiteSpace(lastName)
            || string.IsNullOrWhiteSpace(email)
            || !email.Contains('@'))
        {
            return AddTeamMemberResult.Fail("First name, last name, and a valid email are required.");
        }

        if (!await db.Organizations.AnyAsync(o => o.Id == organizationId, cancellationToken))
        {
            return AddTeamMemberResult.Fail("Organization was not found.");
        }

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role is null)
        {
            return AddTeamMemberResult.Fail("Selected role was not found.");
        }

        if (role.Name == SystemRoles.Owner)
        {
            return AddTeamMemberResult.Fail("Owner role cannot be assigned when adding a team member.");
        }

        if (request.DepartmentId is Guid departmentId)
        {
            var deptOk = await db.Departments.AnyAsync(
                d => d.Id == departmentId && d.OrganizationId == organizationId && d.IsActive,
                cancellationToken);
            if (!deptOk)
            {
                return AddTeamMemberResult.Fail("Selected department was not found.");
            }
        }

        if (request.DesignationId is Guid designationId)
        {
            var desOk = await db.Designations.AnyAsync(
                d => d.Id == designationId && d.OrganizationId == organizationId && d.IsActive,
                cancellationToken);
            if (!desOk)
            {
                return AddTeamMemberResult.Fail("Selected designation was not found.");
            }
        }

        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (existingUser is not null)
        {
            var alreadyMember = await db.OrganizationMembers.AnyAsync(
                m => m.OrganizationId == organizationId && m.UserId == existingUser.Id,
                cancellationToken);
            if (alreadyMember)
            {
                return AddTeamMemberResult.Fail("That email is already on this team.");
            }

            return AddTeamMemberResult.Fail("That email already belongs to another account. Use a unique email.");
        }

        var now = DateTimeOffset.UtcNow;
        var temporaryPassword = $"Welcome@{Random.Shared.Next(100000, 999999)}";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phone,
            PasswordHash = string.Empty,
            IsActive = true,
            CreatedAtUtc = now
        };
        user.PasswordHash = PasswordHasher.HashPassword(user, temporaryPassword);

        var membership = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = user.Id,
            RoleId = role.Id,
            DepartmentId = request.DepartmentId,
            DesignationId = request.DesignationId,
            CreatedAtUtc = now
        };

        db.Users.Add(user);
        db.OrganizationMembers.Add(membership);
        await db.SaveChangesAsync(cancellationToken);

        membership = await db.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.User)
            .Include(m => m.Role)
            .Include(m => m.Department)
            .Include(m => m.Designation)
            .SingleAsync(m => m.Id == membership.Id, cancellationToken);

        return AddTeamMemberResult.Ok(Map(membership), temporaryPassword);
    }

    private static TeamMemberDto Map(OrganizationMember m) =>
        new(
            m.Id,
            m.UserId,
            m.User.FirstName,
            m.User.LastName,
            m.User.Email,
            m.User.PhoneNumber,
            m.Role.Name,
            m.Department?.Name,
            m.Designation?.Name,
            m.User.IsActive,
            m.CreatedAtUtc);
}
