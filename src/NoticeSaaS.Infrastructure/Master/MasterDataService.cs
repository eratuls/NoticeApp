using Microsoft.EntityFrameworkCore;
using NoticeSaaS.Application.Master;
using NoticeSaaS.Domain.Entities;
using NoticeSaaS.Infrastructure.Persistence;

namespace NoticeSaaS.Infrastructure.Master;

public sealed class MasterDataService(NoticeSaaSDbContext db) : IMasterDataService
{
    public async Task<IReadOnlyList<DepartmentDto>> ListDepartmentsAsync(
        Guid organizationId,
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = db.Departments.AsNoTracking()
            .Where(d => d.OrganizationId == organizationId);

        if (activeOnly)
        {
            query = query.Where(d => d.IsActive);
        }

        return await query
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.IsActive, d.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentDto?> CreateDepartmentAsync(
        Guid organizationId,
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (!await db.Organizations.AnyAsync(o => o.Id == organizationId, cancellationToken))
        {
            return null;
        }

        var exists = await db.Departments.AnyAsync(
            d => d.OrganizationId == organizationId && d.Name == name,
            cancellationToken);
        if (exists)
        {
            return null;
        }

        var entity = new Department
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.Departments.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new DepartmentDto(entity.Id, entity.Name, entity.IsActive, entity.CreatedAtUtc);
    }

    public async Task<DepartmentDto?> UpdateDepartmentAsync(
        Guid organizationId,
        Guid departmentId,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var entity = await db.Departments.FirstOrDefaultAsync(
            d => d.Id == departmentId && d.OrganizationId == organizationId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var clash = await db.Departments.AnyAsync(
            d => d.OrganizationId == organizationId && d.Name == name && d.Id != departmentId,
            cancellationToken);
        if (clash)
        {
            return null;
        }

        entity.Name = name;
        entity.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return new DepartmentDto(entity.Id, entity.Name, entity.IsActive, entity.CreatedAtUtc);
    }

    public async Task<bool> DeleteDepartmentAsync(
        Guid organizationId,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.Departments.FirstOrDefaultAsync(
            d => d.Id == departmentId && d.OrganizationId == organizationId,
            cancellationToken);
        if (entity is null)
        {
            return false;
        }

        var members = await db.OrganizationMembers
            .Where(m => m.DepartmentId == departmentId)
            .ToListAsync(cancellationToken);
        foreach (var member in members)
        {
            member.DepartmentId = null;
        }

        db.Departments.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<DesignationDto>> ListDesignationsAsync(
        Guid organizationId,
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = db.Designations.AsNoTracking()
            .Where(d => d.OrganizationId == organizationId);

        if (activeOnly)
        {
            query = query.Where(d => d.IsActive);
        }

        return await query
            .OrderBy(d => d.Name)
            .Select(d => new DesignationDto(d.Id, d.Name, d.IsActive, d.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<DesignationDto?> CreateDesignationAsync(
        Guid organizationId,
        CreateDesignationRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (!await db.Organizations.AnyAsync(o => o.Id == organizationId, cancellationToken))
        {
            return null;
        }

        var exists = await db.Designations.AnyAsync(
            d => d.OrganizationId == organizationId && d.Name == name,
            cancellationToken);
        if (exists)
        {
            return null;
        }

        var entity = new Designation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.Designations.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new DesignationDto(entity.Id, entity.Name, entity.IsActive, entity.CreatedAtUtc);
    }

    public async Task<DesignationDto?> UpdateDesignationAsync(
        Guid organizationId,
        Guid designationId,
        UpdateDesignationRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var entity = await db.Designations.FirstOrDefaultAsync(
            d => d.Id == designationId && d.OrganizationId == organizationId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var clash = await db.Designations.AnyAsync(
            d => d.OrganizationId == organizationId && d.Name == name && d.Id != designationId,
            cancellationToken);
        if (clash)
        {
            return null;
        }

        entity.Name = name;
        entity.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return new DesignationDto(entity.Id, entity.Name, entity.IsActive, entity.CreatedAtUtc);
    }

    public async Task<bool> DeleteDesignationAsync(
        Guid organizationId,
        Guid designationId,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.Designations.FirstOrDefaultAsync(
            d => d.Id == designationId && d.OrganizationId == organizationId,
            cancellationToken);
        if (entity is null)
        {
            return false;
        }

        var members = await db.OrganizationMembers
            .Where(m => m.DesignationId == designationId)
            .ToListAsync(cancellationToken);
        foreach (var member in members)
        {
            member.DesignationId = null;
        }

        db.Designations.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken cancellationToken = default)
    {
        return await db.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsSystem))
            .ToListAsync(cancellationToken);
    }
}
