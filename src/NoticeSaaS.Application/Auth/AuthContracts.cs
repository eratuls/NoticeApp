namespace NoticeSaaS.Application.Auth;

public sealed record LoginRequest(string Email, string Password, bool ForceLogout = false);

public sealed record ActiveSessionInfo(
    string? IpAddress,
    DateTimeOffset LoggedInAtUtc,
    DateTimeOffset LastActivityAtUtc,
    DateTimeOffset ExpiresAtUtc);

public sealed record LoginUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid OrganizationId,
    string OrganizationName);

public sealed record LoginSuccessResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset SessionExpiresAtUtc,
    Guid SessionId,
    LoginUserDto User);

public sealed record SessionConflictResponse(
    string Code,
    string Message,
    ActiveSessionInfo Session);

public sealed record SessionStatusResponse(
    Guid SessionId,
    DateTimeOffset ExpiresAtUtc,
    int RemainingSeconds,
    LoginUserDto User);

public sealed record AuthResult
{
    public bool Succeeded { get; init; }
    public bool Conflict { get; init; }
    public LoginSuccessResponse? Success { get; init; }
    public SessionConflictResponse? ConflictDetails { get; init; }
    public string? Error { get; init; }

    public static AuthResult Ok(LoginSuccessResponse success) =>
        new() { Succeeded = true, Success = success };

    public static AuthResult SessionConflict(SessionConflictResponse conflict) =>
        new() { Conflict = true, ConflictDetails = conflict };

    public static AuthResult Fail(string error) =>
        new() { Error = error };
}
