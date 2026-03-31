using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Epecps.Api.Controllers;

[ApiController]
[Route("api/v1/reviews")]
public class ReviewsController : ControllerBase
{
    // Any signed-in user with API scope
    [HttpGet("{evaluationId}")]
    [Authorize]
    public IActionResult Get(int evaluationId) => Ok(new { evaluationId });

    // Only users who also have the RM app role
    [HttpPost("{evaluationId}/submit-rm")]
    [Authorize(Roles = "RM,SuperAdmin")]
    public IActionResult SubmitByRM(int evaluationId) => NoContent();
}
