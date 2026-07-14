using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NoticeSaaS.Application.Sync;
using NoticeSaaS.Infrastructure.Persistence;
using NoticeSaaS.Infrastructure.Sync;

namespace NoticeSaaS.UnitTests;

public class SyncEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SyncEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
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

    [Fact]
    public async Task VaultClient_PausesForOtp_ThenResumesOnValidOtp()
    {
        await AuthenticateAsync();

        var pan = $"VAULT{Random.Shared.Next(1000, 9999)}A";
        var create = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            module = "IncomeTax",
            syncFrequency = "Weekly",
            portalUsername = pan,
            portalPassword = MockIncomeTaxPortalClient.VaultPassword
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ClientCreatedDto>(JsonOptions);
        Assert.NotNull(created);

        var sync = await _client.PostAsync($"/api/v1/clients/{created.Id}/sync", null);
        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);
        var awaiting = await sync.Content.ReadFromJsonAsync<SyncJobDto>(JsonOptions);
        Assert.NotNull(awaiting);
        Assert.Equal("AwaitingOtp", awaiting.Status);
        Assert.NotNull(awaiting.OtpRequestedAtUtc);
        Assert.Equal(0, awaiting.NoticesUpserted);

        var badOtp = await _client.PostAsJsonAsync(
            $"/api/v1/clients/{created.Id}/sync/{awaiting.Id}/otp",
            new { otp = "000000" });
        Assert.Equal(HttpStatusCode.OK, badOtp.StatusCode);
        var failed = await badOtp.Content.ReadFromJsonAsync<SyncJobDto>(JsonOptions);
        Assert.NotNull(failed);
        Assert.Equal("Failed", failed.Status);
        Assert.Contains("invalid OTP", failed.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        var syncAgain = await _client.PostAsync($"/api/v1/clients/{created.Id}/sync", null);
        Assert.Equal(HttpStatusCode.OK, syncAgain.StatusCode);
        var awaitingAgain = await syncAgain.Content.ReadFromJsonAsync<SyncJobDto>(JsonOptions);
        Assert.NotNull(awaitingAgain);
        Assert.Equal("AwaitingOtp", awaitingAgain.Status);

        var goodOtp = await _client.PostAsJsonAsync(
            $"/api/v1/clients/{created.Id}/sync/{awaitingAgain.Id}/otp",
            new { otp = MockIncomeTaxPortalClient.ValidOtp });
        Assert.Equal(HttpStatusCode.OK, goodOtp.StatusCode);
        var succeeded = await goodOtp.Content.ReadFromJsonAsync<SyncJobDto>(JsonOptions);
        Assert.NotNull(succeeded);
        Assert.Equal("Succeeded", succeeded.Status);
        Assert.Equal(2, succeeded.NoticesUpserted);

        var notices = await _client.GetFromJsonAsync<ClientNoticesDto>(
            $"/api/v1/clients/{created.Id}/notices",
            JsonOptions);
        Assert.NotNull(notices);
        Assert.Contains(notices.Notices, n => n.DocumentReferenceId == $"DIN-SYNC-{pan}-001");
    }

    [Fact]
    public async Task VaultClient_OtpTimeout_FailsCleanly()
    {
        await AuthenticateAsync();

        var pan = $"VTOUT{Random.Shared.Next(1000, 9999)}A";
        var create = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            module = "IncomeTax",
            syncFrequency = "Weekly",
            portalUsername = pan,
            portalPassword = MockIncomeTaxPortalClient.VaultPassword
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ClientCreatedDto>(JsonOptions);
        Assert.NotNull(created);

        var sync = await _client.PostAsync($"/api/v1/clients/{created.Id}/sync", null);
        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);
        var awaiting = await sync.Content.ReadFromJsonAsync<SyncJobDto>(JsonOptions);
        Assert.NotNull(awaiting);
        Assert.Equal("AwaitingOtp", awaiting.Status);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NoticeSaaSDbContext>();
            var job = db.SyncJobs.Single(j => j.Id == awaiting.Id);
            job.OtpRequestedAtUtc = DateTimeOffset.UtcNow - SyncJobProcessor.OtpTimeout - TimeSpan.FromMinutes(1);
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<ISyncJobProcessor>();
            await processor.ProcessPendingAsync(maxJobs: 10);
        }

        var latest = await _client.GetFromJsonAsync<SyncJobDto>(
            $"/api/v1/clients/{created.Id}/sync",
            JsonOptions);
        Assert.NotNull(latest);
        Assert.Equal(awaiting.Id, latest.Id);
        Assert.Equal("Failed", latest.Status);
        Assert.Contains("OTP", latest.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TransientPortalFailure_RetriesThenSucceeds()
    {
        await AuthenticateAsync();

        var pan = $"TRETY{Random.Shared.Next(1000, 9999)}A";
        var create = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            module = "IncomeTax",
            syncFrequency = "Weekly",
            portalUsername = pan,
            portalPassword = MockIncomeTaxPortalClient.TransientOncePassword
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ClientCreatedDto>(JsonOptions);
        Assert.NotNull(created);

        var sync = await _client.PostAsync($"/api/v1/clients/{created.Id}/sync", null);
        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);
        var job = await sync.Content.ReadFromJsonAsync<SyncJobDto>(JsonOptions);
        Assert.NotNull(job);
        Assert.Equal("Succeeded", job.Status);
        Assert.Equal(2, job.NoticesUpserted);
        Assert.Null(job.ErrorMessage);
        Assert.DoesNotContain(MockIncomeTaxPortalClient.TransientOncePassword, job.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task PortalTimeout_FailsWithSafeMessage_WithoutSecrets()
    {
        await AuthenticateAsync();

        var pan = $"PTOUT{Random.Shared.Next(1000, 9999)}A";
        var create = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            module = "IncomeTax",
            syncFrequency = "Weekly",
            portalUsername = pan,
            portalPassword = MockIncomeTaxPortalClient.PortalTimeoutPassword
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ClientCreatedDto>(JsonOptions);
        Assert.NotNull(created);

        var sync = await _client.PostAsync($"/api/v1/clients/{created.Id}/sync", null);
        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);
        var job = await sync.Content.ReadFromJsonAsync<SyncJobDto>(JsonOptions);
        Assert.NotNull(job);
        Assert.Equal("Failed", job.Status);
        Assert.False(string.IsNullOrWhiteSpace(job.ErrorMessage));
        Assert.Contains("portal", job.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(MockIncomeTaxPortalClient.PortalTimeoutPassword, job.ErrorMessage!);
        Assert.DoesNotContain("DemoPortal", job.ErrorMessage!);
        Assert.DoesNotContain(MockIncomeTaxPortalClient.ValidOtp, job.ErrorMessage!);
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

    private sealed record ClientCreatedDto(Guid Id, string Pan);

    private sealed record SyncJobDto(
        Guid Id,
        string Status,
        int NoticesUpserted,
        string? ErrorMessage,
        DateTimeOffset? OtpRequestedAtUtc);

    private sealed record ClientNoticesDto(List<NoticeRow> Notices);

    private sealed record NoticeRow(string? DocumentReferenceId);
}
