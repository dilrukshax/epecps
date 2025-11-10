using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using System.Linq;

namespace Epecps.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    // Require valid token with our scope
    [HttpGet("me")]
    [Authorize]
    [RequiredScope("Epecps.ReadWrite")]
    public IActionResult Me()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Ok(new
        {
            Name = User.Identity?.Name,
            UserId = User.FindFirst("oid")?.Value,      // Entra object id
            Roles = User.FindAll("roles").Select(r => r.Value).ToArray(),
            Claims = claims
        });
    }
}
