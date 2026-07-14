using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NoticeSaaS.Infrastructure.Notices;

public sealed class NoticeAttachmentStorage
{
    private readonly string _root;
    private readonly ILogger<NoticeAttachmentStorage> _logger;

    public NoticeAttachmentStorage(IConfiguration configuration, ILogger<NoticeAttachmentStorage> logger)
    {
        _logger = logger;
        var configured = configuration["Storage:NoticeAttachmentsPath"];
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Path.GetTempPath(), "NoticeSaaS", "notice-attachments")
            : configured;
        Directory.CreateDirectory(_root);
    }

    public string RootPath => _root;

    public async Task<string> SaveAsync(
        Guid noticeId,
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var safeExtension = Path.GetExtension(originalFileName);
        if (safeExtension.Length > 16)
        {
            safeExtension = ".bin";
        }

        var stored = $"{noticeId:N}_{Guid.NewGuid():N}{safeExtension}";
        var path = Path.Combine(_root, stored);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
        _logger.LogDebug("Stored notice attachment {StoredFile} ({Bytes} bytes)", stored, file.Length);
        return stored;
    }

    public Stream? OpenRead(string storedFileName)
    {
        var path = Path.Combine(_root, storedFileName);
        if (!File.Exists(path))
        {
            return null;
        }

        return File.OpenRead(path);
    }

    public async Task EnsureSeedFileAsync(string storedFileName, byte[] bytes, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_root, storedFileName);
        if (File.Exists(path))
        {
            return;
        }

        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
    }
}
