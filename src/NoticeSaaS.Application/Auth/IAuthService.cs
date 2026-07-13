using NoticeSaaS.Application.Auth;

namespace NoticeSaaS.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<SessionStatusResponse?> GetSessionStatusAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<bool> TouchSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
