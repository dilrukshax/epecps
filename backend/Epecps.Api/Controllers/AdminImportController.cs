using Epecps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Epecps.Api.Controllers;

[ApiController]
[Route("api/v1/admin/import")]
[Authorize(Roles = "SuperAdmin")]
public class AdminImportController : ControllerBase
{
    private readonly IUserProjectImportService _importService;

    public AdminImportController(IUserProjectImportService importService)
    {
        _importService = importService;
    }

    [HttpGet("template")]
    public IActionResult DownloadTemplate()
    {
        var bytes = _importService.GenerateTemplate();
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"users-projects-import-template-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    [HttpPost("users-projects")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> ImportUsersProjects(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Excel file is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _importService.ImportAsync(stream, cancellationToken);
        return Ok(result);
    }
}
