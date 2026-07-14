namespace NoticeSaaS.Infrastructure.Notices;

public static class StorageProviders
{
    public const string Local = "Local";
    public const string AzureBlob = "AzureBlob";
}

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Development default: Local disk. Production: AzureBlob.
    /// Valid values: Local | AzureBlob
    /// </summary>
    public string Provider { get; set; } = StorageProviders.Local;

    /// <summary>Used when Provider is Local.</summary>
    public string NoticeAttachmentsPath { get; set; } = string.Empty;

    public AzureBlobStorageOptions AzureBlob { get; set; } = new();
}

public sealed class AzureBlobStorageOptions
{
    /// <summary>
    /// Storage account connection string (prefer Key Vault / env in production).
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Blob container for notice attachments.</summary>
    public string ContainerName { get; set; } = "notice-attachments";
}
