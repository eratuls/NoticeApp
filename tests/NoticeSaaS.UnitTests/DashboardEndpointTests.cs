using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NoticeSaaS.UnitTests;

public class DashboardEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DashboardEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Summary_WithAuth_ReturnsTaskBuckets()
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

        var response = await _client.GetAsync("/api/v1/dashboard/summary?module=IncomeTax&period=Monthly");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<SummaryResponse>();
        Assert.NotNull(summary);
        Assert.True(summary.Clients.Total >= 1);
        Assert.True(summary.Notices.Total >= 1);
        Assert.True(summary.Tasks.New + summary.Tasks.Ongoing + summary.Tasks.Closed >= 1);
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class SummaryResponse
    {
        public CountDelta Clients { get; set; } = new();
        public NoticeSummary Notices { get; set; } = new();
        public TaskBuckets Tasks { get; set; } = new();
    }

    private sealed class CountDelta
    {
        public int Total { get; set; }
    }

    private sealed class NoticeSummary
    {
        public int Total { get; set; }
    }

    private sealed class TaskBuckets
    {
        public int New { get; set; }
        public int Ongoing { get; set; }
        public int Closed { get; set; }
        public int Overdue { get; set; }
    }
}
