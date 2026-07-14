using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoticeSaaS.Application.Clients;
using NoticeSaaS.Domain.Enums;

namespace NoticeSaaS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/clients")]
public class ClientsController(IClientService clientService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? module = "IncomeTax",
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        ComplianceModule? parsedModule = null;
        if (!string.IsNullOrWhiteSpace(module)
            && Enum.TryParse<ComplianceModule>(module, ignoreCase: true, out var m))
        {
            parsedModule = m;
        }

        var items = await clientService.ListAsync(organizationId.Value, parsedModule, search, cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await clientService.CreateAsync(organizationId.Value, request, cancellationToken);
        if (!result.Succeeded || result.Client is null)
        {
            return BadRequest(new { message = result.Error ?? "Unable to create client." });
        }

        return CreatedAtAction(nameof(List), new { module = result.Client.Module }, result.Client);
    }

    [HttpDelete("{clientId:guid}")]
    public async Task<IActionResult> Delete(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await clientService.DeleteAsync(organizationId.Value, clientId, cancellationToken);
        if (!result.Succeeded)
        {
            return NotFound(new { message = result.Error ?? "Client not found." });
        }

        return NoContent();
    }

    private Guid? GetOrganizationId()
    {
        var value = User.FindFirstValue("org_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
