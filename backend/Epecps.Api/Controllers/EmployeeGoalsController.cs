using Epecps.Application.DTOs.EmployeeGoals;
using Epecps.Application.Interfaces;
using Epecps.Application.Exceptions;
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
    /// Get personal goals grouped by goal sets
    /// </summary>
    [HttpGet("my/sets")]
    public async Task<IActionResult> GetMyGoalSets(CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        var goalSets = await _personalGoalService.GetMyGoalSetsAsync(userId, cancellationToken);
        return Ok(goalSets);
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
    /// Start working on a goal after RM approval
    /// Goal must be in ApprovedByRM status
    /// </summary>
    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartGoal(
        Guid id,
        [FromBody] StartGoalRequestDto? dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var result = await _personalGoalService.StartGoalAsync(id, userId, cancellationToken);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Mark a goal as completed
    /// Goal must be in InProgress status
    /// If all goals in the evaluation are completed, triggers workflow continuation to TL review
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteGoal(
        Guid id,
        [FromBody] CompleteGoalRequestDto? dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var result = await _personalGoalService.CompleteGoalAsync(id, userId, dto, cancellationToken);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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
    /// Recalculate goal score based on completed activities
    /// </summary>
    [HttpPost("{id}/recalculate-score")]
    public async Task<IActionResult> RecalculateScore(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        await _personalGoalService.RecalculateGoalScoreFromActivitiesAsync(id, userId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Delete a personal goal (only if not submitted for evaluation)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePersonalGoal(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            await _personalGoalService.DeletePersonalGoalAsync(id, userId, cancellationToken);
            return Ok(new { message = "Personal goal deleted successfully." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete an activity from a personal goal
    /// </summary>
    [HttpDelete("{id}/activities/{activityId}")]
    public async Task<IActionResult> DeleteActivity(
        Guid id,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            await _personalGoalService.DeleteActivityAsync(id, activityId, userId, cancellationToken);
            return Ok(new { message = "Activity deleted successfully." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete an entire goal set (all goals in the set)
    /// </summary>
    [HttpDelete("sets/{goalSetId}")]
    public async Task<IActionResult> DeleteGoalSet(
        Guid goalSetId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            await _personalGoalService.DeleteGoalSetAsync(goalSetId, userId, cancellationToken);
            return Ok(new { message = "Goal set deleted successfully." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Submit a goal set for RM review (starts the approval workflow)
    /// Goals do not need to be completed - they can be submitted in Draft status
    /// </summary>
    [HttpPost("sets/{goalSetId}/submit-for-evaluation")]
    public async Task<IActionResult> SubmitGoalSetForEvaluation(
        Guid goalSetId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var result = await _personalGoalService.SubmitGoalSetForEvaluationAsync(goalSetId, userId, cancellationToken);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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
