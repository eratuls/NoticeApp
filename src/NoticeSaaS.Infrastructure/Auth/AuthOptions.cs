namespace NoticeSaaS.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public JwtOptions Jwt { get; set; } = new();

    public int SessionIdleMinutes { get; set; } = 10;
}

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "NoticeSaaS";
    public string Audience { get; set; } = "NoticeSaaS.Web";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
}
