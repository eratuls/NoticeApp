using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NoticeSaaS.Application.Auth;

namespace NoticeSaaS.Infrastructure.Auth;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(
        LoginUserDto user,
        Guid sessionId);
}

public sealed class JwtTokenService(IOptions<AuthOptions> options) : IJwtTokenService
{
    public (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(
        LoginUserDto user,
        Guid sessionId)
    {
        var jwt = options.Value.Jwt;
        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Auth:Jwt:SigningKey must be at least 32 characters.");
        }

        var expires = DateTimeOffset.UtcNow.AddMinutes(jwt.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("session_id", sessionId.ToString()),
            new("org_id", user.OrganizationId.ToString()),
            new(ClaimTypes.Role, user.Role),
            new("given_name", user.FirstName),
            new("family_name", user.LastName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
