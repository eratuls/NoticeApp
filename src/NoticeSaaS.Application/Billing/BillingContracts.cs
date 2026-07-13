namespace NoticeSaaS.Application.Billing;

public sealed record UsageLimitsDto(
    string PlanName,
    bool IsActive,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset ExpiresAtUtc,
    int DaysRemaining,
    int AssesseeUsed,
    int AssesseeLimit,
    int AssesseeRemaining,
    int SyncCreditsUsed,
    int SyncCreditLimit,
    int SyncCreditsRemaining,
    IReadOnlyList<string> ModulesEnabled,
    string Note);

public interface IUsageLimitsService
{
    Task<UsageLimitsDto> GetAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<(bool Allowed, string? Error)> EnsureCanAddAssesseeAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<(bool Allowed, string? Error)> EnsureHasSyncCreditAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task ConsumeSyncCreditAsync(
        Guid organizationId,
        Guid syncJobId,
        CancellationToken cancellationToken = default);
}
