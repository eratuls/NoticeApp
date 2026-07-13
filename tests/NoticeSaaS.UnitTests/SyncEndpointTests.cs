using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NoticeSaaS.UnitTests;

public class SyncEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SyncEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TriggerManualSync_UpsertsPortalNotices()
    {
        await AuthenticateAsync();

        var clients = await _client.GetFromJsonAsync<ClientRow[]>(
            "/api/v1/clients?module=IncomeTax",
            JsonOptions);
        Assert.NotNull(clients);
        var demo = Assert.Single(clients, c => c.Pan == "AABCM1234F");

        var beforeNotices = await _client.GetFromJsonAsync<ClientNoticesDto>(
            $"/api/v1/clients/{demo.Id}/notices?kind=Notice",
            JsonOptions);
        Assert.NotNull(beforeNotices);
        var beforeCount = beforeNotices.Notices.Count;

        var sync = await _client.PostAsync($"/api/v1/clients/{demo.Id}/sync", null);
        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);
        var job = await sync.Content.ReadFromJsonAsync<SyncJobDto>(JsonOptions);
        Assert.NotNull(job);
        Assert.Equal("Succeeded", job.Status);
        Assert.Equal(2, job.NoticesUpserted);

        var afterNotices = await _client.GetFromJsonAsync<ClientNoticesDto>(
            $"/api/v1/clients/{demo.Id}/notices?kind=Notice",
            JsonOptions);
        Assert.NotNull(afterNotices);
        Assert.True(afterNotices.Notices.Count >= beforeCount);
        Assert.Contains(afterNotices.Notices, n => n.DocumentReferenceId == "DIN-SYNC-AABCM1234F-001");

        var latest = await _client.GetFromJsonAsync<SyncJobDto>(
            $"/api/v1/clients/{demo.Id}/sync",
            JsonOptions);
        Assert.NotNull(latest);
        Assert.Equal(job.Id, latest.Id);

        var refreshed = await _client.GetFromJsonAsync<ClientRow[]>(
            "/api/v1/clients?module=IncomeTax",
            JsonOptions);
        Assert.NotNull(refreshed);
        var updated = Assert.Single(refreshed, c => c.Id == demo.Id);
        Assert.Equal("Succeeded", updated.LatestSyncStatus);
        Assert.NotNull(updated.LastSyncAtUtc);
    }

    [Fact]
    public async Task TriggerSync_IsIdempotentOnDocumentReference()
    {
        await AuthenticateAsync();

        var clients = await _client.GetFromJsonAsync<ClientRow[]>(
            "/api/v1/clients?module=IncomeTax",
            JsonOptions);
        Assert.NotNull(clients);
        var demo = Assert.Single(clients, c => c.Pan == "AABCM1234F");

        var first = await _client.PostAsync($"/api/v1/clients/{demo.Id}/sync", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var noticesAfterFirst = await _client.GetFromJsonAsync<ClientNoticesDto>(
            $"/api/v1/clients/{demo.Id}/notices",
            JsonOptions);
        Assert.NotNull(noticesAfterFirst);
        var countAfterFirst = noticesAfterFirst.Notices.Count;

        var second = await _client.PostAsync($"/api/v1/clients/{demo.Id}/sync", null);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var job = await second.Content.ReadFromJsonAsync<SyncJobDto>(JsonOptions);
        Assert.NotNull(job);
        Assert.Equal("Succeeded", job.Status);

        var noticesAfterSecond = await _client.GetFromJsonAsync<ClientNoticesDto>(
            $"/api/v1/clients/{demo.Id}/notices",
            JsonOptions);
        Assert.NotNull(noticesAfterSecond);
        Assert.Equal(countAfterFirst, noticesAfterSecond.Notices.Count);
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

    private sealed record ClientRow(
        Guid Id,
        string Pan,
        DateTimeOffset? LastSyncAtUtc,
        string? LatestSyncStatus);

    private sealed record SyncJobDto(
        Guid Id,
        string Status,
        int NoticesUpserted);

    private sealed record ClientNoticesDto(List<NoticeRow> Notices);

    private sealed record NoticeRow(string? DocumentReferenceId);
}
