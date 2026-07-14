using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NoticeSaaS.UnitTests;

public class NoticeWrapupEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NoticeWrapupEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Assign_Upload_Download_And_ManualNotice_Work()
    {
        await AuthenticateAsync();

        var clients = await _client.GetFromJsonAsync<ClientDto[]>(
            "/api/v1/clients?module=IncomeTax",
            JsonOptions);
        Assert.NotNull(clients);
        var demo = Assert.Single(clients, c => c.Pan == "AABCM1234F");

        var notices = await _client.GetFromJsonAsync<ClientNoticesDto>(
            $"/api/v1/clients/{demo.Id}/notices?kind=Notice",
            JsonOptions);
        Assert.NotNull(notices);
        Assert.NotEmpty(notices.Notices);
        var noticeId = notices.Notices[0].Id;

        var team = await _client.GetFromJsonAsync<TeamListDto>("/api/v1/team", JsonOptions);
        Assert.NotNull(team);
        Assert.NotEmpty(team.Members);
        var assignee = team.Members[0];

        var assign = await _client.PatchAsJsonAsync(
            $"/api/v1/notices/{noticeId}/assign",
            new { assignedToUserId = assignee.UserId });
        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
        var assigned = await assign.Content.ReadFromJsonAsync<NoticeDetailDto>(JsonOptions);
        Assert.NotNull(assigned);
        Assert.Equal(assignee.UserId, assigned.AssignedToUserId);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Reply"), "category");
        var fileBytes = Encoding.UTF8.GetBytes("reply body");
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "reply-note.txt");

        var upload = await _client.PostAsync($"/api/v1/notices/{noticeId}/attachments", content);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var attachment = await upload.Content.ReadFromJsonAsync<AttachmentDto>(JsonOptions);
        Assert.NotNull(attachment);
        Assert.Equal("Reply", attachment.Category);

        var download = await _client.GetAsync(
            $"/api/v1/notices/{noticeId}/attachments/{attachment.Id}/download");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        var bytes = await download.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);

        var detail = await _client.GetFromJsonAsync<NoticeDetailDto>(
            $"/api/v1/notices/{noticeId}",
            JsonOptions);
        Assert.NotNull(detail);
        Assert.Contains(detail.Attachments, a => a.Category == "Reply");

        var manual = await _client.PostAsJsonAsync(
            $"/api/v1/clients/{demo.Id}/notices/manual",
            new
            {
                section = "Manual",
                description = "Day12 offline letter",
                financialYear = "2024-25",
                documentReferenceId = "MAN-1001",
                responseDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd")
            });
        Assert.Equal(HttpStatusCode.OK, manual.StatusCode);

        var manualTab = await _client.GetFromJsonAsync<ClientNoticesDto>(
            $"/api/v1/clients/{demo.Id}/notices?kind=Manual",
            JsonOptions);
        Assert.NotNull(manualTab);
        Assert.Contains(manualTab.Notices, n => n.Description.Contains("Day12 offline letter")
            || n.Description.Contains("CA letter"));
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
        public List<NoticeListDto> Notices { get; set; } = [];
    }

    private sealed class NoticeListDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    private sealed class TeamListDto
    {
        public List<TeamMemberDto> Members { get; set; } = [];
    }

    private sealed class TeamMemberDto
    {
        public Guid UserId { get; set; }
    }

    private sealed class NoticeDetailDto
    {
        public Guid? AssignedToUserId { get; set; }
        public List<AttachmentDto> Attachments { get; set; } = [];
    }

    private sealed class AttachmentDto
    {
        public Guid Id { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}
