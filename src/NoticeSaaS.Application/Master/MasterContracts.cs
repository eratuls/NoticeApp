namespace NoticeSaaS.Application.Master;

public sealed record DepartmentDto(Guid Id, string Name, bool IsActive, DateTimeOffset CreatedAtUtc);

public sealed record CreateDepartmentRequest(string Name);

public sealed record UpdateDepartmentRequest(string Name, bool IsActive);

public sealed record DesignationDto(Guid Id, string Name, bool IsActive, DateTimeOffset CreatedAtUtc);

public sealed record CreateDesignationRequest(string Name);

public sealed record UpdateDesignationRequest(string Name, bool IsActive);

public sealed record RoleDto(Guid Id, string Name, string? Description, bool IsSystem);

public interface IMasterDataService
{
    Task<IReadOnlyList<DepartmentDto>> ListDepartmentsAsync(
        Guid organizationId,
        bool activeOnly = false,
        CancellationToken cancellationToken = default);

    Task<DepartmentDto?> CreateDepartmentAsync(
        Guid organizationId,
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<DepartmentDto?> UpdateDepartmentAsync(
        Guid organizationId,
        Guid departmentId,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDepartmentAsync(
        Guid organizationId,
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DesignationDto>> ListDesignationsAsync(
        Guid organizationId,
        bool activeOnly = false,
        CancellationToken cancellationToken = default);

    Task<DesignationDto?> CreateDesignationAsync(
        Guid organizationId,
        CreateDesignationRequest request,
        CancellationToken cancellationToken = default);

    Task<DesignationDto?> UpdateDesignationAsync(
        Guid organizationId,
        Guid designationId,
        UpdateDesignationRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDesignationAsync(
        Guid organizationId,
        Guid designationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken cancellationToken = default);
}
