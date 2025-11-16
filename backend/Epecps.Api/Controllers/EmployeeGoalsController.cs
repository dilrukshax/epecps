using Epecps.Application.DTOs.EmployeeGoals;
using Epecps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Epecps.Api.Controllers;

/// <summary>
/// Controller for managing employee personal goals
/// </summary>
[ApiController]
[Route("api/employee-goals")]
[Authorize]
public class EmployeeGoalsController : ControllerBase
{
    private readonly IPersonalGoalService _personalGoalService;
    private readonly IUserSyncService _userSyncService;

    public EmployeeGoalsController(IPersonalGoalService personalGoalService, IUserSyncService userSyncService)
    {
        _personalGoalService = personalGoalService;
        _userSyncService = userSyncService;
    }

    /// <summary>
    /// Test endpoint to verify user sync and return current user info
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var email = User.FindFirst("preferred_username")?.Value
                ?? User.FindFirst("email")?.Value
                ?? User.FindFirst("upn")?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value;
            
            var fullName = User.FindFirst("name")?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value
                ?? "Unknown";

            return Ok(new
            {
                userId,
                email,
                fullName,
                message = "User successfully synced to database!"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, innerError = ex.InnerException?.Message });
        }
    }

    /// <summary>
    /// Create a new personal goal for the authenticated user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePersonalGoal(
        [FromBody] CreatePersonalGoalDto dto,
        CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        var goalId = await _personalGoalService.CreatePersonalGoalAsync(userId, dto, cancellationToken);
        
        return CreatedAtAction(
            nameof(GetGoalDetails),
            new { id = goalId },
            new { id = goalId });
    }

    /// <summary>
    /// Get all personal goals for the current user
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyGoals(CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        var goals = await _personalGoalService.GetMyGoalsAsync(userId, cancellationToken);
        return Ok(goals);
    }

    /// <summary>
    /// Get detailed information about a specific personal goal
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetGoalDetails(Guid id, CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        var goal = await _personalGoalService.GetGoalDetailsAsync(id, userId, cancellationToken);
        return Ok(goal);
    }

    /// <summary>
    /// Update a personal goal
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePersonalGoal(
        Guid id,
        [FromBody] UpdatePersonalGoalDto dto,
        CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        await _personalGoalService.UpdatePersonalGoalAsync(id, userId, dto, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Update the score/progress of a personal goal
    /// </summary>
    [HttpPut("{id}/score")]
    public async Task<IActionResult> UpdateGoalScore(
        Guid id,
        [FromBody] UpdatePersonalGoalScoreDto dto,
        CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        await _personalGoalService.UpdateGoalScoreAsync(id, userId, dto, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Add a new activity to a personal goal
    /// </summary>
    [HttpPost("{id}/activities")]
    public async Task<IActionResult> AddActivity(
        Guid id,
        [FromBody] CreatePersonalGoalActivityDto dto,
        CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        var activityId = await _personalGoalService.AddActivityAsync(id, userId, dto, cancellationToken);
        return Ok(new { id = activityId });
    }

    /// <summary>
    /// Update an existing activity
    /// </summary>
    [HttpPut("{id}/activities/{activityId}")]
    public async Task<IActionResult> UpdateActivity(
        Guid id,
        Guid activityId,
        [FromBody] UpdatePersonalGoalActivityDto dto,
        CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        await _personalGoalService.UpdateActivityAsync(id, activityId, userId, dto, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Helper method to get the authenticated user ID from JWT claims
    /// Auto-creates user in database if doesn't exist
    /// </summary>
    private async Task<int> GetAuthenticatedUserIdAsync(CancellationToken cancellationToken = default)
    {
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
