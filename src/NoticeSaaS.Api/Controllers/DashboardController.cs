using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoticeSaaS.Application.Dashboard;

namespace NoticeSaaS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/dashboard")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(
        [FromQuery] string module = "IncomeTax",
        [FromQuery] string period = "Monthly",
        CancellationToken cancellationToken = default)
    {
        var orgClaim = User.FindFirstValue("org_id");
        if (!Guid.TryParse(orgClaim, out var organizationId))
        {
            return Unauthorized();
        }

        var summary = await dashboardService.GetSummaryAsync(
            organizationId,
            new DashboardSummaryRequest(module, period),
            cancellationToken);

        return summary is null ? NotFound() : Ok(summary);
    }
}
