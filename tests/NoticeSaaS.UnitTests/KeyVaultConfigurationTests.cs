using NoticeSaaS.Infrastructure.Configuration;

namespace NoticeSaaS.UnitTests;

public class KeyVaultConfigurationTests
{
    [Theory]
    [InlineData("ConnectionStrings:Default", "ConnectionStrings--Default")]
    [InlineData("Auth:Jwt:SigningKey", "Auth--Jwt--SigningKey")]
    [InlineData("Storage:AzureBlob:ConnectionString", "Storage--AzureBlob--ConnectionString")]
    public void ToKeyVaultSecretName_MapsNestedKeys(string configurationKey, string expectedSecretName)
    {
        Assert.Equal(expectedSecretName, KeyVaultConfigurationExtensions.ToKeyVaultSecretName(configurationKey));
    }

    [Fact]
    public void KeyVaultOptions_Default_IsDisabledForLocalDev()
    {
        var options = new KeyVaultOptions();
        Assert.False(options.Enabled);
        Assert.Equal(string.Empty, options.VaultUri);
    }
}
