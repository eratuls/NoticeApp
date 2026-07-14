using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoticeSaaS.Application.Master;

namespace NoticeSaaS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/master")]
public class MasterController(IMasterDataService masterDataService) : ControllerBase
{
    [HttpGet("departments")]
    public async Task<IActionResult> ListDepartments(
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await masterDataService.ListDepartmentsAsync(
            organizationId.Value,
            activeOnly,
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("departments")]
    public async Task<IActionResult> CreateDepartment(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await masterDataService.CreateDepartmentAsync(
            organizationId.Value,
            request,
            cancellationToken);
        return result is null
            ? BadRequest(new { message = "Unable to create department. Check the name is unique." })
            : Ok(result);
    }

    [HttpPut("departments/{departmentId:guid}")]
    public async Task<IActionResult> UpdateDepartment(
        Guid departmentId,
        [FromBody] UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await masterDataService.UpdateDepartmentAsync(
            organizationId.Value,
            departmentId,
            request,
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("departments/{departmentId:guid}")]
    public async Task<IActionResult> DeleteDepartment(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var ok = await masterDataService.DeleteDepartmentAsync(
            organizationId.Value,
            departmentId,
            cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("designations")]
    public async Task<IActionResult> ListDesignations(
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await masterDataService.ListDesignationsAsync(
            organizationId.Value,
            activeOnly,
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("designations")]
    public async Task<IActionResult> CreateDesignation(
        [FromBody] CreateDesignationRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await masterDataService.CreateDesignationAsync(
            organizationId.Value,
            request,
            cancellationToken);
        return result is null
            ? BadRequest(new { message = "Unable to create designation. Check the name is unique." })
            : Ok(result);
    }

    [HttpPut("designations/{designationId:guid}")]
    public async Task<IActionResult> UpdateDesignation(
        Guid designationId,
        [FromBody] UpdateDesignationRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var result = await masterDataService.UpdateDesignationAsync(
            organizationId.Value,
            designationId,
            request,
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("designations/{designationId:guid}")]
    public async Task<IActionResult> DeleteDesignation(
        Guid designationId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        if (organizationId is null)
        {
            return Unauthorized();
        }

        var ok = await masterDataService.DeleteDesignationAsync(
            organizationId.Value,
            designationId,
            cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("roles")]
    public async Task<IActionResult> ListRoles(CancellationToken cancellationToken = default)
    {
        if (GetOrganizationId() is null)
        {
            return Unauthorized();
        }

        var result = await masterDataService.ListRolesAsync(cancellationToken);
        return Ok(result);
    }

    private Guid? GetOrganizationId()
    {
        var value = User.FindFirstValue("org_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
