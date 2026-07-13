namespace NoticeSaaS.Application.Notices;

public sealed record NoticeListItemDto(
    Guid Id,
    Guid ClientId,
    string Kind,
    string Section,
    string Description,
    string? FinancialYear,
    string? ProceedingId,
    string? DocumentReferenceId,
    string Status,
    bool IsOverdue,
    DateOnly? ServedDate,
    DateOnly? ResponseDueDate,
    DateTimeOffset CreatedAtUtc);

public sealed record NoticeCommentDto(
    Guid Id,
    string AuthorName,
    string Body,
    DateTimeOffset CreatedAtUtc);

public sealed record NoticeStatusEventDto(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    string? Note,
    DateTimeOffset CreatedAtUtc);

public sealed record NoticeDetailDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string ClientPan,
    string Kind,
    string Module,
    string Section,
    string Description,
    string? FinancialYear,
    string? ProceedingId,
    string? DocumentReferenceId,
    string Status,
    bool IsOverdue,
    DateOnly? ServedDate,
    DateOnly? ResponseDueDate,
    DateOnly? ResponseSubmittedDate,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<NoticeCommentDto> Comments,
    IReadOnlyList<NoticeStatusEventDto> Timeline);

public sealed record ClientNoticesResponse(
    Guid ClientId,
    string ClientName,
    string ClientPan,
    bool IsActive,
    IReadOnlyDictionary<string, int> KindCounts,
    IReadOnlyList<NoticeListItemDto> Notices);

public sealed record UpdateNoticeStatusRequest(string Status, string? Note = null);

public sealed record AddNoticeCommentRequest(string Body);

public interface INoticeService
{
    Task<ClientNoticesResponse?> ListForClientAsync(
        Guid organizationId,
        Guid clientId,
        string? kind,
        string? search,
        CancellationToken cancellationToken = default);

    Task<NoticeDetailDto?> GetAsync(
        Guid organizationId,
        Guid noticeId,
        CancellationToken cancellationToken = default);

    Task<NoticeDetailDto?> UpdateStatusAsync(
        Guid organizationId,
        Guid noticeId,
        Guid userId,
        UpdateNoticeStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<NoticeCommentDto?> AddCommentAsync(
        Guid organizationId,
        Guid noticeId,
        Guid userId,
        AddNoticeCommentRequest request,
        CancellationToken cancellationToken = default);
}
