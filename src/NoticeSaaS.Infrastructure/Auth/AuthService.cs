using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NoticeSaaS.Application.Auth;
using NoticeSaaS.Domain.Entities;
using NoticeSaaS.Infrastructure.Persistence;

namespace NoticeSaaS.Infrastructure.Auth;

public sealed class AuthService(
    NoticeSaaSDbContext db,
    IJwtTokenService jwtTokenService,
    IOptions<AuthOptions> options) : IAuthService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<AuthResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return AuthResult.Fail("Email and password are required.");
        }

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return AuthResult.Fail("Invalid email or password.");
        }

        var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            return AuthResult.Fail("Invalid email or password.");
        }

        var trackedUser = await db.Users.FirstAsync(u => u.Id == user.Id, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var activeSession = await db.UserSessions
            .Where(s => s.UserId == trackedUser.Id && s.RevokedAtUtc == null && s.ExpiresAtUtc > now)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSession is not null && !request.ForceLogout)
        {
            return AuthResult.SessionConflict(new SessionConflictResponse(
                Code: "SESSION_ACTIVE",
                Message: "Another session is already active for this account.",
                Session: new ActiveSessionInfo(
                    activeSession.IpAddress,
                    activeSession.CreatedAtUtc,
                    activeSession.LastActivityAtUtc,
                    activeSession.ExpiresAtUtc)));
        }

        if (activeSession is not null && request.ForceLogout)
        {
            activeSession.RevokedAtUtc = now;
        }

        var membership = await db.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.Organization)
            .Include(m => m.Role)
            .Where(m => m.UserId == trackedUser.Id)
            .OrderBy(m => m.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is null)
        {
            return AuthResult.Fail("User is not a member of any organization.");
        }

        var idle = TimeSpan.FromMinutes(Math.Max(1, options.Value.SessionIdleMinutes));
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = trackedUser.Id,
            IpAddress = Truncate(ipAddress, 64),
            UserAgent = Truncate(userAgent, 512),
            CreatedAtUtc = now,
            LastActivityAtUtc = now,
            ExpiresAtUtc = now.Add(idle)
        };

        db.UserSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        var userDto = new LoginUserDto(
            trackedUser.Id,
            trackedUser.Email,
            trackedUser.FirstName,
            trackedUser.LastName,
            membership.Role.Name,
            membership.OrganizationId,
            membership.Organization.Name);

        var (token, tokenExpires) = jwtTokenService.CreateAccessToken(userDto, session.Id);

        return AuthResult.Ok(new LoginSuccessResponse(
            token,
            tokenExpires,
            session.ExpiresAtUtc,
            session.Id,
            userDto));
    }

    public async Task LogoutAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await db.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null || session.RevokedAtUtc is not null)
        {
            return;
        }

        session.RevokedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SessionStatusResponse?> GetSessionStatusAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var session = await db.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null || !session.IsActive(now))
        {
            return null;
        }

        var membership = await db.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.Organization)
            .Include(m => m.Role)
            .Include(m => m.User)
            .Where(m => m.UserId == session.UserId)
            .OrderBy(m => m.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is null)
        {
            return null;
        }

        var remaining = (int)Math.Max(0, (session.ExpiresAtUtc - now).TotalSeconds);
        var userDto = new LoginUserDto(
            membership.User.Id,
            membership.User.Email,
            membership.User.FirstName,
            membership.User.LastName,
            membership.Role.Name,
            membership.OrganizationId,
            membership.Organization.Name);

        return new SessionStatusResponse(session.Id, session.ExpiresAtUtc, remaining, userDto);
    }

    public async Task<bool> TouchSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var session = await db.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null || !session.IsActive(now))
        {
            return false;
        }

        var idle = TimeSpan.FromMinutes(Math.Max(1, options.Value.SessionIdleMinutes));
        session.LastActivityAtUtc = now;
        session.ExpiresAtUtc = now.Add(idle);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? value : value.Length <= max ? value : value[..max];
}
