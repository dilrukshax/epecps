using Epecps.Application.DTOs.EmployeeGoals;
using Epecps.Application.Interfaces;
using Epecps.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Epecps.Api.Controllers;

/// <summary>
/// Controller for RM (Reporting Manager) goal assignment operations.
/// RM browses the goal library and assigns goals to employees.
/// </summary>
[ApiController]
[Route("api/rm/goals")]
[Authorize]
public class RmGoalAssignmentController : ControllerBase
{
    private readonly IRmGoalAssignmentService _rmGoalAssignmentService;
    private readonly IUserSyncService _userSyncService;

    public RmGoalAssignmentController(IRmGoalAssignmentService rmGoalAssignmentService, IUserSyncService userSyncService)
    {
        _rmGoalAssignmentService = rmGoalAssignmentService;
        _userSyncService = userSyncService;
    }

    /// <summary>
    /// Get all goals from the system goal library.
    /// Returns a flat list of all active ScoreItems with category/template info.
    /// </summary>
    [HttpGet("library")]
    public async Task<IActionResult> GetGoalLibrary(CancellationToken cancellationToken)
    {
        try
        {
            var goals = await _rmGoalAssignmentService.GetGoalLibraryAsync(cancellationToken);
            return Ok(goals);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get employees that this RM can assign goals to.
    /// </summary>
    [HttpGet("employees")]
    public async Task<IActionResult> GetMyEmployees(CancellationToken cancellationToken)
    {
        try
        {
            var rmUserId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var employees = await _rmGoalAssignmentService.GetMyEmployeesAsync(rmUserId, cancellationToken);
            return Ok(employees);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Assign goals to an employee.
    /// Creates PersonalGoal records and an evaluation in Approved_By_RM status.
    /// </summary>
    [HttpPost("assign")]
    public async Task<IActionResult> AssignGoalsToEmployee(
        [FromBody] RmAssignGoalsDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var rmUserId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var result = await _rmGoalAssignmentService.AssignGoalsToEmployeeAsync(rmUserId, dto, cancellationToken);
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
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
            return BadRequest(new { error = $"Database error: {innerMessage}" });
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            return BadRequest(new { error = innerMessage });
        }
    }

    /// <summary>
    /// Get all goal assignments made by this RM.
    /// </summary>
    [HttpGet("assignments")]
    public async Task<IActionResult> GetMyAssignments(CancellationToken cancellationToken)
    {
        try
        {
            var rmUserId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var assignments = await _rmGoalAssignmentService.GetMyAssignmentsAsync(rmUserId, cancellationToken);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get goal assignments for a specific employee.
    /// </summary>
    [HttpGet("assignments/employee/{employeeUserId}")]
    public async Task<IActionResult> GetAssignmentsForEmployee(
        int employeeUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            var rmUserId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var assignments = await _rmGoalAssignmentService.GetAssignmentsForEmployeeAsync(rmUserId, employeeUserId, cancellationToken);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Helper method to get the authenticated user ID from JWT claims
    /// </summary>
    private async Task<int> GetAuthenticatedUserIdAsync(CancellationToken cancellationToken = default)
    {
        var email = User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value
            ?? User.FindFirst("upn")?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            var availableClaims = string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"));
            throw new UnauthorizedAccessException($"User email not found in authentication token. Available claims: {availableClaims}");
        }

        var fullName = User.FindFirst("name")?.Value
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst(ClaimTypes.GivenName)?.Value
            ?? email.Split('@')[0];

        var userId = await _userSyncService.SyncUserFromClaimsAsync(email, fullName, cancellationToken);
        return userId;
    }
}
