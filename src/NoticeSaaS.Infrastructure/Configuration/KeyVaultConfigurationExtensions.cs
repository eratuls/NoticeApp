using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace NoticeSaaS.Infrastructure.Configuration;

public static class KeyVaultConfigurationExtensions
{
    /// <summary>
    /// Loads secrets from Azure Key Vault when <c>KeyVault:Enabled</c> is true and
    /// <c>KeyVault:VaultUri</c> is set. Otherwise no-ops so local development keeps using
    /// appsettings / environment variables / user-secrets.
    /// </summary>
    /// <remarks>
    /// Store secrets with <c>--</c> for nesting, e.g.
    /// <c>ConnectionStrings--Default</c>, <c>Auth--Jwt--SigningKey</c>,
    /// <c>Storage--AzureBlob--ConnectionString</c>.
    /// </remarks>
    public static IConfigurationManager AddOptionalAzureKeyVault(this IConfigurationManager configuration)
    {
        var options = configuration.GetSection(KeyVaultOptions.SectionName).Get<KeyVaultOptions>()
            ?? new KeyVaultOptions();

        if (!options.Enabled || string.IsNullOrWhiteSpace(options.VaultUri))
        {
            return configuration;
        }

        if (!Uri.TryCreate(options.VaultUri.Trim(), UriKind.Absolute, out var vaultUri))
        {
            throw new InvalidOperationException(
                $"KeyVault:VaultUri '{options.VaultUri}' is not a valid absolute URI.");
        }

        configuration.AddAzureKeyVault(vaultUri, new DefaultAzureCredential());
        return configuration;
    }

    /// <summary>Maps an ASP.NET configuration key to the Key Vault secret name convention.</summary>
    public static string ToKeyVaultSecretName(string configurationKey) =>
        configurationKey.Replace(":", "--", StringComparison.Ordinal);
}
