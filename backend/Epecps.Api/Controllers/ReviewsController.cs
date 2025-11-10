using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace Epecps.Api.Controllers;

[ApiController]
[Route("api/v1/reviews")]
public class ReviewsController : ControllerBase
{
    // Any signed-in user with API scope
    [HttpGet("{evaluationId}")]
    [Authorize]
    [RequiredScope("Epecps.ReadWrite")]
    public IActionResult Get(int evaluationId) => Ok(new { evaluationId });

    // Only users who also have the RM app role
    [HttpPost("{evaluationId}/submit-rm")]
    [Authorize(Roles = "RM")]
    [RequiredScope("Epecps.ReadWrite")]
    public IActionResult SubmitByRM(int evaluationId) => NoContent();
}
