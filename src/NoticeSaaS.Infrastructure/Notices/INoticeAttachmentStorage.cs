namespace NoticeSaaS.Infrastructure.Notices;

public interface INoticeAttachmentStorage
{
    Task<string> SaveAsync(
        Guid noticeId,
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string storedFileName,
        CancellationToken cancellationToken = default);

    Task EnsureSeedFileAsync(
        string storedFileName,
        byte[] bytes,
        CancellationToken cancellationToken = default);
}
