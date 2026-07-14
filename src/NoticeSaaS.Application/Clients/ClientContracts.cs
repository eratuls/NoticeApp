using NoticeSaaS.Domain.Enums;

namespace NoticeSaaS.Application.Clients;

public sealed record ClientListItemDto(
    Guid Id,
    string Name,
    string Pan,
    string? AadhaarMasked,
    string? CaPan,
    string Module,
    string SyncFrequency,
    string? PortalUsername,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastSyncAtUtc,
    DateTimeOffset? NextSyncAtUtc,
    int NoticeCount,
    string? LatestSyncStatus = null,
    string? LatestSyncError = null,
    int? LatestNoticesUpserted = null);

/// <summary>
/// Add client. For Income Tax, only portal username/password (+ sync) are required;
/// name, PAN, and Aadhaar are fetched from the Income Tax portal when credentials are valid.
/// </summary>
public sealed record CreateClientRequest(
    string Module,
    string SyncFrequency,
    string PortalUsername,
    string PortalPassword,
    string? Name = null,
    string? Pan = null,
    string? CaPan = null);

public sealed record CreateClientResult(bool Succeeded, ClientListItemDto? Client, string? Error)
{
    public static CreateClientResult Ok(ClientListItemDto client) => new(true, client, null);
    public static CreateClientResult Fail(string error) => new(false, null, error);
}

public sealed record DeleteClientResult(bool Succeeded, string? Error)
{
    public static DeleteClientResult Ok() => new(true, null);
    public static DeleteClientResult Fail(string error) => new(false, error);
}

public interface IClientService
{
    Task<IReadOnlyList<ClientListItemDto>> ListAsync(
        Guid organizationId,
        ComplianceModule? module,
        string? search,
        CancellationToken cancellationToken = default);

    Task<CreateClientResult> CreateAsync(
        Guid organizationId,
        CreateClientRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently removes the client and all related data (credentials, notices, sync jobs, reminders).
    /// </summary>
    Task<DeleteClientResult> DeleteAsync(
        Guid organizationId,
        Guid clientId,
        CancellationToken cancellationToken = default);
}
