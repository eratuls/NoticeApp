using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NoticeSaaS.UnitTests;

public class TeamMasterEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TeamMasterEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MasterDepartments_Crud_And_TeamAddMember_Work()
    {
        await AuthenticateAsync();

        var departments = await _client.GetFromJsonAsync<DepartmentDto[]>(
            "/api/v1/master/departments",
            JsonOptions);
        Assert.NotNull(departments);
        Assert.Contains(departments, d => d.Name == "Income Tax");
        Assert.Contains(departments, d => d.Name == "GST");
        Assert.Contains(departments, d => d.Name == "TDS");
        Assert.Contains(departments, d => d.Name == "Accounting");

        var createDept = await _client.PostAsJsonAsync("/api/v1/master/departments", new
        {
            name = "Audit Desk"
        });
        Assert.Equal(HttpStatusCode.OK, createDept.StatusCode);
        var createdDept = await createDept.Content.ReadFromJsonAsync<DepartmentDto>(JsonOptions);
        Assert.NotNull(createdDept);
        Assert.Equal("Audit Desk", createdDept.Name);

        var updateDept = await _client.PutAsJsonAsync(
            $"/api/v1/master/departments/{createdDept.Id}",
            new { name = "Audit Desk Updated", isActive = true });
        Assert.Equal(HttpStatusCode.OK, updateDept.StatusCode);

        var designations = await _client.GetFromJsonAsync<DesignationDto[]>(
            "/api/v1/master/designations?activeOnly=true",
            JsonOptions);
        Assert.NotNull(designations);
        Assert.NotEmpty(designations);

        var roles = await _client.GetFromJsonAsync<RoleDto[]>(
            "/api/v1/master/roles",
            JsonOptions);
        Assert.NotNull(roles);
        var staffRole = Assert.Single(roles, r => r.Name == "Staff");

        var incomeTax = Assert.Single(departments, d => d.Name == "Income Tax");
        var associate = Assert.Single(designations, d => d.Name == "Associate");

        var addMember = await _client.PostAsJsonAsync("/api/v1/team", new
        {
            firstName = "Priya",
            lastName = "Shah",
            email = $"priya.day11.{Guid.NewGuid():N}@example.com",
            phoneNumber = "9876543210",
            roleId = staffRole.Id,
            departmentId = incomeTax.Id,
            designationId = associate.Id
        });
        Assert.Equal(HttpStatusCode.OK, addMember.StatusCode);
        var added = await addMember.Content.ReadFromJsonAsync<AddMemberResponse>(JsonOptions);
        Assert.NotNull(added);
        Assert.NotNull(added.Member);
        Assert.False(string.IsNullOrWhiteSpace(added.TemporaryPassword));
        Assert.Equal("Income Tax", added.Member.Department);
        Assert.Equal("Associate", added.Member.Designation);
        Assert.Equal("Staff", added.Member.Role);

        var team = await _client.GetFromJsonAsync<TeamListDto>("/api/v1/team", JsonOptions);
        Assert.NotNull(team);
        Assert.True(team.Total >= 2);
        Assert.Contains(team.Members, m => m.Email == added.Member.Email);

        var filtered = await _client.GetFromJsonAsync<TeamListDto>(
            "/api/v1/team?role=Staff&search=Priya",
            JsonOptions);
        Assert.NotNull(filtered);
        Assert.Contains(filtered.Members, m => m.Email == added.Member.Email);

        var delete = await _client.DeleteAsync($"/api/v1/master/departments/{createdDept.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
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

    private sealed class DepartmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class DesignationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TeamListDto
    {
        public int Total { get; set; }
        public List<TeamMemberDto> Members { get; set; } = [];
    }

    private sealed class TeamMemberDto
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? Designation { get; set; }
    }

    private sealed class AddMemberResponse
    {
        public TeamMemberDto? Member { get; set; }
        public string? TemporaryPassword { get; set; }
    }
}
