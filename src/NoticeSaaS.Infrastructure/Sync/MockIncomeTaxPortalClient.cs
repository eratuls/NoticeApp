using NoticeSaaS.Application.Sync;
using NoticeSaaS.Domain.Enums;

namespace NoticeSaaS.Infrastructure.Sync;

/// <summary>
/// Password-only portal adapter for local/dev. Simulates a successful Income Tax login
/// and returns deterministic notices so upsert/dedupe can be tested without Playwright.
/// Swap for a Playwright implementation when vault/login go/no-go passes.
/// </summary>
public sealed class MockIncomeTaxPortalClient : IIncomeTaxPortalClient
{
    public Task<IReadOnlyList<PortalNoticeDto>> FetchNoticesAsync(
        PortalLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidOperationException("Portal username and password are required.");
        }

        if (string.Equals(request.Password, "wrong-password", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Income Tax portal login failed: invalid password.");
        }

        var pan = request.Pan.ToUpperInvariant();
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

        return Task.FromResult(notices);
    }
}
