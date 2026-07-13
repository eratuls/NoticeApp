using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NoticeSaaS.UnitTests;

public class ClientsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ClientsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListAndCreate_WorkForAuthenticatedUser()
    {
        await AuthenticateAsync();

        var list = await _client.GetAsync("/api/v1/clients?module=IncomeTax");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var existing = await list.Content.ReadFromJsonAsync<List<ClientDto>>();
        Assert.NotNull(existing);
        Assert.Contains(existing, c => c.Pan == "AABCM1234F");

        var pan = $"ZZZZZ{Random.Shared.Next(1000, 9999)}A";
        var create = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            name = "Day5 Test Client",
            pan,
            module = "IncomeTax",
            syncFrequency = "Weekly",
            portalUsername = pan,
            portalPassword = "PortalPass@123"
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ClientDto>();
        Assert.NotNull(created);
        Assert.Equal(pan, created.Pan);
        Assert.Equal("Weekly", created.SyncFrequency);
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
        var auth = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(auth);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class ClientDto
    {
        public string Pan { get; set; } = string.Empty;
        public string SyncFrequency { get; set; } = string.Empty;
    }
}
