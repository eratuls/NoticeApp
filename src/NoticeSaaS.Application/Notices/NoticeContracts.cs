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
    DateTimeOffset CreatedAtUtc,
    string? AssignedToName,
    bool HasNoticeDocument);

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

public sealed record NoticeAttachmentDto(
    Guid Id,
    string Category,
    string FileName,
    string ContentType,
    long SizeBytes,
    string UploadedByName,
    DateTimeOffset CreatedAtUtc,
    string DownloadUrl);

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
    Guid? AssignedToUserId,
    string? AssignedToName,
    IReadOnlyList<NoticeCommentDto> Comments,
    IReadOnlyList<NoticeStatusEventDto> Timeline,
    IReadOnlyList<NoticeAttachmentDto> Attachments);

public sealed record ClientNoticesResponse(
    Guid ClientId,
    string ClientName,
    string ClientPan,
    bool IsActive,
    IReadOnlyDictionary<string, int> KindCounts,
    IReadOnlyList<NoticeListItemDto> Notices);

public sealed record UpdateNoticeStatusRequest(string Status, string? Note = null);

public sealed record AddNoticeCommentRequest(string Body);

public sealed record AssignNoticeRequest(Guid? AssignedToUserId);

public sealed record CreateManualNoticeRequest(
    string Section,
    string Description,
    string? FinancialYear,
    string? DocumentReferenceId,
    DateOnly? ServedDate,
    DateOnly? ResponseDueDate);

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

    Task<NoticeDetailDto?> AssignAsync(
        Guid organizationId,
        Guid noticeId,
        AssignNoticeRequest request,
        CancellationToken cancellationToken = default);

    Task<NoticeListItemDto?> CreateManualAsync(
        Guid organizationId,
        Guid clientId,
        CreateManualNoticeRequest request,
        CancellationToken cancellationToken = default);

    Task<NoticeAttachmentDto?> UploadAttachmentAsync(
        Guid organizationId,
        Guid noticeId,
        Guid userId,
        string category,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<(Stream Stream, string ContentType, string FileName)?> OpenAttachmentAsync(
        Guid organizationId,
        Guid noticeId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);
}
