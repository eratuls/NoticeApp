namespace NoticeSaaS.Application.Team;

public sealed record TeamMemberDto(
    Guid MembershipId,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Role,
    string? Department,
    string? Designation,
    bool IsActive,
    DateTimeOffset JoinedAtUtc);

public sealed record TeamListResponse(
    int Total,
    int Active,
    int Inactive,
    IReadOnlyList<TeamMemberDto> Members);

public sealed record AddTeamMemberRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    Guid RoleId,
    Guid? DepartmentId,
    Guid? DesignationId);

public sealed record AddTeamMemberResult(
    bool Succeeded,
    string? Error,
    TeamMemberDto? Member,
    string? TemporaryPassword)
{
    public static AddTeamMemberResult Ok(TeamMemberDto member, string temporaryPassword) =>
        new(true, null, member, temporaryPassword);

    public static AddTeamMemberResult Fail(string error) =>
        new(false, error, null, null);
}

public interface ITeamService
{
    Task<TeamListResponse> ListAsync(
        Guid organizationId,
        string? role = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<AddTeamMemberResult> AddAsync(
        Guid organizationId,
        AddTeamMemberRequest request,
        CancellationToken cancellationToken = default);
}
