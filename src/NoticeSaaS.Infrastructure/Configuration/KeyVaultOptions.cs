namespace NoticeSaaS.Infrastructure.Configuration;

public sealed class KeyVaultOptions
{
    public const string SectionName = "KeyVault";

    /// <summary>
    /// When false (default for local/dev), appsettings / env vars are used and Key Vault is skipped.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>Key Vault URI, e.g. https://my-vault.vault.azure.net/</summary>
    public string VaultUri { get; set; } = string.Empty;
}
