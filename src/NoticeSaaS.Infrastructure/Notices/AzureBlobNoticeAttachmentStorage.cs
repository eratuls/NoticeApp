using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NoticeSaaS.Infrastructure.Notices;

public sealed class AzureBlobNoticeAttachmentStorage : INoticeAttachmentStorage
{
    private readonly BlobContainerClient _container;
    private readonly ILogger<AzureBlobNoticeAttachmentStorage> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public AzureBlobNoticeAttachmentStorage(
        IOptions<StorageOptions> options,
        ILogger<AzureBlobNoticeAttachmentStorage> logger)
    {
        _logger = logger;
        var blob = options.Value.AzureBlob
            ?? throw new InvalidOperationException("Storage:AzureBlob configuration is missing.");

        if (string.IsNullOrWhiteSpace(blob.ConnectionString))
        {
            throw new InvalidOperationException(
                "Storage:AzureBlob:ConnectionString is required when Storage:Provider is AzureBlob.");
        }

        if (string.IsNullOrWhiteSpace(blob.ContainerName))
        {
            throw new InvalidOperationException(
                "Storage:AzureBlob:ContainerName is required when Storage:Provider is AzureBlob.");
        }

        _container = new BlobContainerClient(blob.ConnectionString, blob.ContainerName.Trim());
    }

    public async Task<string> SaveAsync(
        Guid noticeId,
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerAsync(cancellationToken);

        var stored = LocalNoticeAttachmentStorage.BuildStoredFileName(noticeId, originalFileName);
        var blob = _container.GetBlobClient(stored);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = GuessContentType(originalFileName)
                }
            },
            cancellationToken);

        _logger.LogDebug("Stored Azure Blob notice attachment {StoredFile}", stored);
        return stored;
    }

    public async Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        await EnsureContainerAsync(cancellationToken);

        var blob = _container.GetBlobClient(storedFileName);
        if (!await blob.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task EnsureSeedFileAsync(
        string storedFileName,
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerAsync(cancellationToken);

        var blob = _container.GetBlobClient(storedFileName);
        if (await blob.ExistsAsync(cancellationToken))
        {
            return;
        }

        await using var stream = new MemoryStream(bytes);
        await blob.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/pdf" }
            },
            cancellationToken);
    }

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            await _container.CreateIfNotExistsAsync(
                PublicAccessType.None,
                cancellationToken: cancellationToken);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static string GuessContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
}
