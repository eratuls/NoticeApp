using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NoticeSaaS.UnitTests;

public class RemindersEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RemindersEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListCreateCompleteAndNotifications_Work()
    {
        await AuthenticateAsync();

        var listResponse = await _client.GetAsync("/api/v1/reminders?status=Pending");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<ReminderListDto>(JsonOptions);
        Assert.NotNull(list);
        Assert.True(list.PendingCount >= 1);

        var notices = await _client.GetFromJsonAsync<ClientListDto[]>("/api/v1/clients?module=IncomeTax");
        Assert.NotNull(notices);
        var demo = Assert.Single(notices, c => c.Pan == "AABCM1234F");
        var clientNotices = await _client.GetFromJsonAsync<ClientNoticesDto>(
            $"/api/v1/clients/{demo.Id}/notices?kind=Notice",
            JsonOptions);
        Assert.NotNull(clientNotices);
        Assert.NotEmpty(clientNotices.Notices);

        var create = await _client.PostAsJsonAsync("/api/v1/reminders", new
        {
            noticeId = clientNotices.Notices[0].Id,
            description = "Day7 test reminder",
            priority = "High",
            dueOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)).ToString("yyyy-MM-dd")
        });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ReminderItemDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Day7 test reminder", created.Description);

        var complete = await _client.PostAsync($"/api/v1/reminders/{created.Id}/complete", null);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        var notifications = await _client.GetFromJsonAsync<NotificationListDto>(
            "/api/v1/notifications",
            JsonOptions);
        Assert.NotNull(notifications);
        Assert.True(notifications.UnreadCount >= 1);
        Assert.NotEmpty(notifications.Notifications);

        var markAll = await _client.PostAsync("/api/v1/notifications/read-all", null);
        Assert.Equal(HttpStatusCode.OK, markAll.StatusCode);
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

    private sealed class ReminderListDto
    {
        public int PendingCount { get; set; }
    }

    private sealed class ReminderItemDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    private sealed class ClientListDto
    {
        public Guid Id { get; set; }
        public string Pan { get; set; } = string.Empty;
    }

    private sealed class ClientNoticesDto
    {
        public List<NoticeItemDto> Notices { get; set; } = [];
    }

    private sealed class NoticeItemDto
    {
        public Guid Id { get; set; }
    }

    private sealed class NotificationListDto
    {
        public int UnreadCount { get; set; }
        public List<object> Notifications { get; set; } = [];
    }
}
