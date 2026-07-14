using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NoticeSaaS.Application.Sync;
using NoticeSaaS.Domain.Enums;

namespace NoticeSaaS.Infrastructure.Sync;

/// <summary>
/// Portal adapter for local/dev.
/// <list type="bullet">
/// <item>Any non-empty password except <c>wrong-password</c> is treated as valid login.</item>
/// <item>Password <c>vault-otp</c> requires OTP <c>123456</c> only when fetching notices.</item>
/// <item>Password <c>transient-once</c> fails once with a transient error, then succeeds (retry tests).</item>
/// <item>Password <c>portal-timeout</c> always fails with a transient portal timeout.</item>
/// <item>Profile (name, PAN, Aadhaar) is derived from the portal username (usually PAN).</item>
/// </list>
/// Swap for a Playwright / real e-Filing implementation when vault/login go/no-go passes.
/// </summary>
public sealed class MockIncomeTaxPortalClient : IIncomeTaxPortalClient
{
    public const string VaultPassword = "vault-otp";
    public const string ValidOtp = "123456";
    public const string InvalidPassword = "wrong-password";
    public const string TransientOncePassword = "transient-once";
    public const string PortalTimeoutPassword = "portal-timeout";

    private static readonly ConcurrentDictionary<string, int> TransientFailCounts = new(StringComparer.Ordinal);

    public Task<PortalProfileDto> LoginAndGetProfileAsync(
        PortalCredentialsRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateCredentials(request.Username, request.Password);

        var pan = ResolvePan(request.Username);
        var name = ResolveName(pan, request.Username);
        var aadhaarMasked = MaskAadhaar(DeriveAadhaarDigits(pan));

        return Task.FromResult(new PortalProfileDto(name, pan, aadhaarMasked));
    }

    public async Task<IReadOnlyList<PortalNoticeDto>> FetchNoticesAsync(
        PortalLoginRequest request,
        string? otp = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateCredentials(request.Username, request.Password);

        // Simulate a slow portal call boundary without blocking tests for 30s.
        await Task.Delay(1, cancellationToken);

        if (string.Equals(request.Password, PortalTimeoutPassword, StringComparison.Ordinal))
        {
            throw new PortalTransientException("Income Tax portal timed out. Try sync again.");
        }

        if (string.Equals(request.Password, TransientOncePassword, StringComparison.Ordinal))
        {
            var key = $"{request.Username}|{TransientOncePassword}";
            var fails = TransientFailCounts.AddOrUpdate(key, 1, (_, n) => n + 1);
            if (fails == 1)
            {
                throw new PortalTransientException("Income Tax portal is temporarily unavailable.");
            }
        }

        var requiresVault = string.Equals(request.Password, VaultPassword, StringComparison.Ordinal);
        if (requiresVault)
        {
            if (string.IsNullOrWhiteSpace(otp))
            {
                throw new PortalOtpRequiredException();
            }

            if (!string.Equals(otp.Trim(), ValidOtp, StringComparison.Ordinal))
            {
                throw new PortalAuthException("Income Tax portal login failed: invalid OTP.");
            }
        }

        var pan = string.IsNullOrWhiteSpace(request.Pan)
            ? ResolvePan(request.Username)
            : request.Pan.Trim().ToUpperInvariant();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        IReadOnlyList<PortalNoticeDto> notices =
        [
            new PortalNoticeDto(
                Section: "143(2)",
                Description: $"Synced scrutiny notice for {request.ClientName} ({pan})",
                FinancialYear: "2024-25",
                ProceedingId: $"SYNC-{pan}-PROC",
                DocumentReferenceId: $"DIN-SYNC-{pan}-001",
                Kind: NoticeKind.Notice.ToString(),
                ServedDate: today.AddDays(-10),
                ResponseDueDate: today.AddDays(20),
                PdfUrl: null),
            new PortalNoticeDto(
                Section: "Order",
                Description: $"Synced direct order for {request.ClientName} ({pan})",
                FinancialYear: "2024-25",
                ProceedingId: $"SYNC-{pan}-ORD",
                DocumentReferenceId: $"DIN-SYNC-{pan}-002",
                Kind: NoticeKind.DirectOrder.ToString(),
                ServedDate: today.AddDays(-5),
                ResponseDueDate: null,
                PdfUrl: null)
        ];

        return notices;
    }

    private static void ValidateCredentials(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new PortalAuthException("Portal username and password are required.");
        }

        if (string.Equals(password, InvalidPassword, StringComparison.Ordinal))
        {
            throw new PortalAuthException("Income Tax portal login failed: invalid username or password.");
        }
    }

    /// <summary>Income Tax user ID is typically the PAN; otherwise derive a stable demo PAN.</summary>
    internal static string ResolvePan(string username)
    {
        var trimmed = username.Trim().ToUpperInvariant();
        if (trimmed.Length == 10 && trimmed.All(char.IsLetterOrDigit))
        {
            return trimmed;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(trimmed));
        var digits = new StringBuilder(5);
        for (var i = 0; i < hash.Length && digits.Length < 4; i++)
        {
            digits.Append((hash[i] % 10).ToString(CultureInfo.InvariantCulture));
        }

        var letters = new string(trimmed.Where(char.IsLetter).Take(5).ToArray()).PadRight(5, 'X');
        if (letters.Length > 5)
        {
            letters = letters[..5];
        }

        return $"{letters.ToUpperInvariant()}{digits}A";
    }

    private static string ResolveName(string pan, string username)
    {
        if (string.Equals(pan, "AABCM1234F", StringComparison.Ordinal))
        {
            return "Marshal Quarries And Granites Pvt Ltd";
        }

        return $"Assessee {username.Trim()}";
    }

    private static string DeriveAadhaarDigits(string pan)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"aadhaar:{pan}"));
        var digits = new StringBuilder(12);
        for (var i = 0; i < hash.Length && digits.Length < 12; i++)
        {
            digits.Append((hash[i] % 10).ToString(CultureInfo.InvariantCulture));
        }

        return digits.ToString();
    }

    private static string MaskAadhaar(string digits) =>
        digits.Length >= 4
            ? $"XXXX-XXXX-{digits[^4..]}"
            : "XXXX-XXXX-XXXX";
}
