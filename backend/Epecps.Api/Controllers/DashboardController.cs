using Epecps.Application.DTOs.Dashboard;
using Epecps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Epecps.Api.Controllers;

/// <summary>
/// Controller for dashboard statistics and data
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;
    private readonly IUserSyncService _userSyncService;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger,
        IUserSyncService userSyncService)
    {
        _dashboardService = dashboardService;
        _logger = logger;
        _userSyncService = userSyncService;
    }

    /// <summary>
    /// Get comprehensive dashboard data for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDataDto>> GetDashboardData(CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var data = await _dashboardService.GetDashboardDataAsync(userId, cancellationToken);
            return Ok(data);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access to dashboard");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data");
            return StatusCode(500, new { error = "Failed to load dashboard data", details = ex.Message });
        }
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats(CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var stats = await _dashboardService.GetDashboardStatsAsync(userId, cancellationToken);
            return Ok(stats);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access to dashboard stats");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard statistics");
            return StatusCode(500, new { error = "Failed to load statistics", details = ex.Message });
        }
    }

    /// <summary>
    /// Get latest activities
    /// </summary>
    [HttpGet("activities")]
    public async Task<ActionResult<List<LatestActivityDto>>> GetLatestActivities(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var activities = await _dashboardService.GetLatestActivitiesAsync(userId, count, cancellationToken);
            return Ok(activities);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access to activities");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest activities");
            return StatusCode(500, new { error = "Failed to load activities", details = ex.Message });
        }
    }

    /// <summary>
    /// Helper method to get the authenticated user ID from JWT claims
    /// Auto-creates user in database if doesn't exist
    /// </summary>
    private async Task<int> GetAuthenticatedUserIdAsync(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst("userId")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out var parsedUserId))
        {
            return parsedUserId;
        }

        // Azure AD tokens typically use "preferred_username" or "email" for the user's email
        var email = User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value
            ?? User.FindFirst("upn")?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            // If no email found, throw error with available claims for debugging
            var availableClaims = string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"));
            throw new UnauthorizedAccessException($"User email not found in authentication token. Available claims: {availableClaims}");
        }

        // Get full name from claims
        var fullName = User.FindFirst("name")?.Value
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst(ClaimTypes.GivenName)?.Value
            ?? email.Split('@')[0];

        // Sync user to database (creates if doesn't exist, returns existing if found)
        var userId = await _userSyncService.SyncUserFromClaimsAsync(email, fullName, cancellationToken);

        return userId;
    }
}
