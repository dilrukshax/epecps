using Epecps.Application.DTOs.Reports;
using Epecps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Epecps.Api.Controllers;

/// <summary>
/// Controller for HR reports
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly Epecps.Infrastructure.Persistence.EpecpsDbContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService reportService,
        Epecps.Infrastructure.Persistence.EpecpsDbContext context,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get evaluation report data (for preview)
    /// </summary>
    [HttpPost("evaluations/data")]
    public async Task<IActionResult> GetEvaluationReportData(
        [FromBody] EvaluationReportFilterDto filter,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("GetEvaluationReportData called with filter: {@Filter}", filter);
            
            // Temporarily removed role check for testing - allow all authenticated users
            // TODO: Re-enable role check: if (!await UserHasRoleAsync("HR", cancellationToken))
            
            var data = await _reportService.GetEvaluationReportDataAsync(filter, cancellationToken);
            
            _logger.LogInformation("Returning {Count} evaluation records", data.Count);
            
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetEvaluationReportData");
            return StatusCode(500, new { error = "Failed to get report data.", details = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Download evaluation report as Excel
    /// </summary>
    [HttpPost("evaluations/download")]
    public async Task<IActionResult> DownloadEvaluationReport(
        [FromBody] EvaluationReportFilterDto filter,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("DownloadEvaluationReport called");
            
            // Temporarily removed role check for testing - allow all authenticated users
            // TODO: Re-enable role check: if (!await UserHasRoleAsync("HR", cancellationToken))

            var excelBytes = await _reportService.GenerateEvaluationExcelReportAsync(filter, cancellationToken);
            
            var fileName = $"Evaluation_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            
            _logger.LogInformation("Generated Excel file: {FileName}, Size: {Size} bytes", fileName, excelBytes.Length);
            
            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DownloadEvaluationReport");
            return StatusCode(500, new { error = "Failed to generate report.", details = ex.Message });
        }
    }

    /// <summary>
    /// Get available cycles for filtering
    /// </summary>
    [HttpGet("cycles")]
    public async Task<IActionResult> GetCycles(CancellationToken cancellationToken)
    {
        try
        {
            var cycles = await _context.Cycles
                .OrderByDescending(c => c.StartDate)
                .Select(c => new { c.CycleId, c.Name, c.StartDate, c.EndDate, c.Status })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} cycles", cycles.Count);
            return Ok(cycles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cycles");
            return StatusCode(500, new { error = "Failed to get cycles.", details = ex.Message });
        }
    }

    /// <summary>
    /// Get available departments for filtering
    /// </summary>
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
    {
        try
        {
            var departments = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new { d.DeptId, d.Name })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} departments", departments.Count);
            return Ok(departments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting departments");
            return StatusCode(500, new { error = "Failed to get departments.", details = ex.Message });
        }
    }

    /// <summary>
    /// Get database statistics for debugging
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetDatabaseStats(CancellationToken cancellationToken)
    {
        try
        {
            var stats = new
            {
                TotalEvaluations = await _context.Evaluations.CountAsync(cancellationToken),
                TotalUsers = await _context.Users.CountAsync(cancellationToken),
                TotalCycles = await _context.Cycles.CountAsync(cancellationToken),
                TotalDepartments = await _context.Departments.CountAsync(cancellationToken),
                TotalReviews = await _context.Reviews.CountAsync(cancellationToken),
                EvaluationsByCycle = await _context.Evaluations
                    .GroupBy(e => e.Cycle.Name)
                    .Select(g => new { Cycle = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken)
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database stats");
            return StatusCode(500, new { error = "Failed to get stats.", details = ex.Message });
        }
    }

    /// <summary>
    /// Helper method to check if user has a specific role
    /// </summary>
    private async Task<bool> UserHasRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var email = User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value
            ?? User.FindFirst("upn")?.Value;

        if (string.IsNullOrEmpty(email)) return false;

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null) return false;

        return user.UserRoles.Any(ur => ur.Role.Name == roleName);
    }
}
