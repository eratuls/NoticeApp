using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NoticeSaaS.Infrastructure.Persistence;

namespace NoticeSaaS.UnitTests;

public class UsageEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UsageEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsage_ReturnsDemoPlanQuotas()
    {
        await AuthenticateAsync();

        var usage = await _client.GetFromJsonAsync<UsageDto>("/api/v1/usage", JsonOptions);
        Assert.NotNull(usage);
        Assert.Equal("Demo Plan", usage.PlanName);
        Assert.True(usage.IsActive);
        Assert.Equal(50, usage.AssesseeLimit);
        Assert.Equal(150, usage.SyncCreditLimit);
        Assert.True(usage.AssesseeUsed >= 1);
        Assert.True(usage.SyncCreditsRemaining >= 0);
        Assert.Contains(usage.ModulesEnabled, m => m.Contains("IncomeTax", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Sync_ConsumesOneCredit()
    {
        await AuthenticateAsync();

        var before = await _client.GetFromJsonAsync<UsageDto>("/api/v1/usage", JsonOptions);
        Assert.NotNull(before);

        var clients = await _client.GetFromJsonAsync<ClientRow[]>(
            "/api/v1/clients?module=IncomeTax",
            JsonOptions);
        Assert.NotNull(clients);
        var demo = Assert.Single(clients, c => c.Pan == "AABCM1234F");

        var sync = await _client.PostAsync($"/api/v1/clients/{demo.Id}/sync", null);
        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);

        var after = await _client.GetFromJsonAsync<UsageDto>("/api/v1/usage", JsonOptions);
        Assert.NotNull(after);
        Assert.Equal(before.SyncCreditsUsed + 1, after.SyncCreditsUsed);
        Assert.Equal(before.SyncCreditsRemaining - 1, after.SyncCreditsRemaining);
    }

    [Fact]
    public async Task Sync_BlockedWhenCreditsExhausted()
    {
        await AuthenticateAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NoticeSaaSDbContext>();
            var sub = db.OrganizationSubscriptions.Single(
                s => s.OrganizationId == Guid.Parse("11111111-1111-1111-1111-111111111111"));
            sub.SyncCreditsUsed = sub.SyncCreditLimit;
            await db.SaveChangesAsync();
        }

        try
        {
            var clients = await _client.GetFromJsonAsync<ClientRow[]>(
                "/api/v1/clients?module=IncomeTax",
                JsonOptions);
            Assert.NotNull(clients);
            var demo = Assert.Single(clients, c => c.Pan == "AABCM1234F");

            var sync = await _client.PostAsync($"/api/v1/clients/{demo.Id}/sync", null);
            Assert.Equal(HttpStatusCode.BadRequest, sync.StatusCode);
            var body = await sync.Content.ReadAsStringAsync();
            Assert.Contains("credit", body, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NoticeSaaSDbContext>();
            var sub = db.OrganizationSubscriptions.Single(
                s => s.OrganizationId == Guid.Parse("11111111-1111-1111-1111-111111111111"));
            sub.SyncCreditsUsed = 15;
            await db.SaveChangesAsync();
        }
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

    private sealed record LoginResponse(string AccessToken);

    private sealed record UsageDto(
        string PlanName,
        bool IsActive,
        int AssesseeUsed,
        int AssesseeLimit,
        int SyncCreditsUsed,
        int SyncCreditLimit,
        int SyncCreditsRemaining,
        List<string> ModulesEnabled);

    private sealed record ClientRow(Guid Id, string Pan);
}
