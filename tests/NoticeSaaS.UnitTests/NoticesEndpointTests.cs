using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NoticeSaaS.UnitTests;

public class NoticesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NoticesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListDetailStatusAndComment_WorkForAuthenticatedUser()
    {
        await AuthenticateAsync();

        var clients = await _client.GetFromJsonAsync<List<ClientDto>>("/api/v1/clients?module=IncomeTax");
        Assert.NotNull(clients);
        var demo = Assert.Single(clients, c => c.Pan == "AABCM1234F");

        var listResponse = await _client.GetAsync($"/api/v1/clients/{demo.Id}/notices?kind=Notice");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<ClientNoticesDto>(JsonOptions);
        Assert.NotNull(list);
        Assert.True(list.Notices.Count >= 1);
        Assert.True(list.KindCounts.GetValueOrDefault("Notice") >= 1);
        Assert.True(list.KindCounts.GetValueOrDefault("DirectOrder") >= 1);

        var first = list.Notices[0];
        var detailResponse = await _client.GetAsync($"/api/v1/notices/{first.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<NoticeDetailDto>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(demo.Id, detail.ClientId);

        var statusResponse = await _client.PatchAsJsonAsync($"/api/v1/notices/{first.Id}/status", new
        {
            status = "InProgress",
            note = "Day6 status update"
        });
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var updated = await statusResponse.Content.ReadFromJsonAsync<NoticeDetailDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("InProgress", updated.Status);
        Assert.Contains(updated.Timeline, e => e.ToStatus == "InProgress");

        var commentResponse = await _client.PostAsJsonAsync($"/api/v1/notices/{first.Id}/comments", new
        {
            body = "Day6 internal remark"
        });
        Assert.Equal(HttpStatusCode.OK, commentResponse.StatusCode);
        var comment = await commentResponse.Content.ReadFromJsonAsync<NoticeCommentDto>(JsonOptions);
        Assert.NotNull(comment);
        Assert.Equal("Day6 internal remark", comment.Body);
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
        public Guid Id { get; set; }
        public string Pan { get; set; } = string.Empty;
    }

    private sealed class ClientNoticesDto
    {
        public List<NoticeListItemDto> Notices { get; set; } = [];
        public Dictionary<string, int> KindCounts { get; set; } = new();
    }

    private sealed class NoticeListItemDto
    {
        public Guid Id { get; set; }
    }

    private sealed class NoticeDetailDto
    {
        public Guid ClientId { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<TimelineDto> Timeline { get; set; } = [];
    }

    private sealed class TimelineDto
    {
        public string ToStatus { get; set; } = string.Empty;
    }

    private sealed class NoticeCommentDto
    {
        public string Body { get; set; } = string.Empty;
    }
}
