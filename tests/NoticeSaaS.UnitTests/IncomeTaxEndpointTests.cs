using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NoticeSaaS.Infrastructure.Sync;

namespace NoticeSaaS.UnitTests;

public class IncomeTaxEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _client;

    public IncomeTaxEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ReturnsPanAndAadhaar_WhenCredentialsValid()
    {
        await AuthenticateAsync();

        var username = $"LOGIN{Random.Shared.Next(1000, 9999)}A";
        var response = await _client.PostAsJsonAsync("/api/v1/income-tax/login", new
        {
            username,
            password = "PortalPass@123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal(username, profile.Pan);
        Assert.Equal($"Assessee {username}", profile.Name);
        Assert.StartsWith("XXXX-XXXX-", profile.AadhaarMasked);
    }

    [Fact]
    public async Task Login_RejectsInvalidPassword()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/income-tax/login", new
        {
            username = "AABCM1234F",
            password = MockIncomeTaxPortalClient.InvalidPassword
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task AuthenticateAsync()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@noticesaas.local",
            password = "Admin@12345",
            forceLogout = true
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(auth);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class ProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string Pan { get; set; } = string.Empty;
        public string AadhaarMasked { get; set; } = string.Empty;
    }
}
