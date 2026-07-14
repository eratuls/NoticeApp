using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NoticeSaaS.Infrastructure.Notices;

public sealed class LocalNoticeAttachmentStorage : INoticeAttachmentStorage
{
    private readonly string _root;
    private readonly ILogger<LocalNoticeAttachmentStorage> _logger;

    public LocalNoticeAttachmentStorage(
        IOptions<StorageOptions> options,
        ILogger<LocalNoticeAttachmentStorage> logger)
    {
        _logger = logger;
        var configured = options.Value.NoticeAttachmentsPath;
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Path.GetTempPath(), "NoticeSaaS", "notice-attachments")
            : configured;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(
        Guid noticeId,
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var stored = BuildStoredFileName(noticeId, originalFileName);
        var path = Path.Combine(_root, stored);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
        _logger.LogDebug("Stored local notice attachment {StoredFile} ({Bytes} bytes)", stored, file.Length);
        return stored;
    }

    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(_root, storedFileName);
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(File.OpenRead(path));
    }

    public async Task EnsureSeedFileAsync(
        string storedFileName,
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_root, storedFileName);
        if (File.Exists(path))
        {
            return;
        }

        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
    }

    internal static string BuildStoredFileName(Guid noticeId, string originalFileName)
    {
        var safeExtension = Path.GetExtension(originalFileName);
        if (safeExtension.Length > 16)
        {
            safeExtension = ".bin";
        }

        return $"{noticeId:N}_{Guid.NewGuid():N}{safeExtension}";
    }
}
