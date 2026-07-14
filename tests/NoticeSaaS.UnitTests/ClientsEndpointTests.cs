using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NoticeSaaS.Infrastructure.Sync;

namespace NoticeSaaS.UnitTests;

public class ClientsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
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
        var existing = await list.Content.ReadFromJsonAsync<List<ClientDto>>(JsonOptions);
        Assert.NotNull(existing);
        Assert.Contains(existing, c => c.Pan == "AABCM1234F");

        var pan = $"ZZZZZ{Random.Shared.Next(1000, 9999)}A";
        var create = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            module = "IncomeTax",
            syncFrequency = "Weekly",
            portalUsername = pan,
            portalPassword = "PortalPass@123"
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ClientDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(pan, created.Pan);
        Assert.Equal($"Assessee {pan}", created.Name);
        Assert.False(string.IsNullOrWhiteSpace(created.AadhaarMasked));
        Assert.StartsWith("XXXX-XXXX-", created.AadhaarMasked);
        Assert.Equal("Weekly", created.SyncFrequency);
    }

    [Fact]
    public async Task Create_RejectsInvalidIncomeTaxCredentials()
    {
        await AuthenticateAsync();

        var pan = $"BADPW{Random.Shared.Next(1000, 9999)}A";
        var create = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            module = "IncomeTax",
            syncFrequency = "Weekly",
            portalUsername = pan,
            portalPassword = MockIncomeTaxPortalClient.InvalidPassword
        });

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        var body = await create.Content.ReadFromJsonAsync<ErrorBody>(JsonOptions);
        Assert.NotNull(body);
        Assert.Contains("invalid", body.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_RemovesClientAndNotices()
    {
        await AuthenticateAsync();

        var pan = $"DELTE{Random.Shared.Next(1000, 9999)}A";
        var create = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            module = "IncomeTax",
            syncFrequency = "Weekly",
            portalUsername = pan,
            portalPassword = "PortalPass@123"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ClientDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.False(string.IsNullOrWhiteSpace(created.Id));

        var sync = await _client.PostAsync($"/api/v1/clients/{created.Id}/sync", null);
        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);

        var noticesBefore = await _client.GetAsync($"/api/v1/clients/{created.Id}/notices");
        Assert.Equal(HttpStatusCode.OK, noticesBefore.StatusCode);

        var delete = await _client.DeleteAsync($"/api/v1/clients/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var list = await _client.GetFromJsonAsync<List<ClientDto>>("/api/v1/clients?module=IncomeTax", JsonOptions);
        Assert.NotNull(list);
        Assert.DoesNotContain(list, c => c.Id == created.Id);

        var noticesAfter = await _client.GetAsync($"/api/v1/clients/{created.Id}/notices");
        Assert.Equal(HttpStatusCode.NotFound, noticesAfter.StatusCode);

        var deleteAgain = await _client.DeleteAsync($"/api/v1/clients/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deleteAgain.StatusCode);
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

    private sealed class ClientDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Pan { get; set; } = string.Empty;
        public string? AadhaarMasked { get; set; }
        public string SyncFrequency { get; set; } = string.Empty;
    }

    private sealed class ErrorBody
    {
        public string Message { get; set; } = string.Empty;
    }
}
