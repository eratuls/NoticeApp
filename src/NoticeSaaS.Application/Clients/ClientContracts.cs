using NoticeSaaS.Domain.Enums;

namespace NoticeSaaS.Application.Clients;

public sealed record ClientListItemDto(
    Guid Id,
    string Name,
    string Pan,
    string? CaPan,
    string Module,
    string SyncFrequency,
    string? PortalUsername,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastSyncAtUtc,
    DateTimeOffset? NextSyncAtUtc,
    int NoticeCount);

public sealed record CreateClientRequest(
    string Name,
    string Pan,
    string Module,
    string SyncFrequency,
    string PortalUsername,
    string PortalPassword,
    string? CaPan = null);

public sealed record CreateClientResult(bool Succeeded, ClientListItemDto? Client, string? Error)
{
    public static CreateClientResult Ok(ClientListItemDto client) => new(true, client, null);
    public static CreateClientResult Fail(string error) => new(false, null, error);
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
}
