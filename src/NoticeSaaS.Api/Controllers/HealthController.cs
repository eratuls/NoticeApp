using Microsoft.AspNetCore.Mvc;

namespace NoticeSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            service = "NoticeSaaS.Api",
            utc = DateTime.UtcNow
        });
    }
}
