using Epecps.Application.DTOs.Evaluations;
using Epecps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Epecps.Api.Controllers;

/// <summary>
/// Controller for managing evaluation approvals and workflow
/// </summary>
[ApiController]
[Route("api/evaluations")]
[Authorize]
public class EvaluationsController : ControllerBase
{
    private readonly IEvaluationWorkflowService _evaluationWorkflowService;
    private readonly IUserSyncService _userSyncService;

    public EvaluationsController(
        IEvaluationWorkflowService evaluationWorkflowService,
        IUserSyncService userSyncService)
    {
        _evaluationWorkflowService = evaluationWorkflowService;
        _userSyncService = userSyncService;
    }

    /// <summary>
    /// Get all pending approvals for the current user
    /// </summary>
    [HttpGet("pending-approvals")]
    public async Task<IActionResult> GetPendingApprovals(CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        var pendingApprovals = await _evaluationWorkflowService.GetPendingApprovalsForUserAsync(userId, cancellationToken);
        return Ok(pendingApprovals);
    }

    /// <summary>
    /// Get all evaluations where current user is involved (pending + completed)
    /// </summary>
    [HttpGet("my-evaluations")]
    public async Task<IActionResult> GetMyEvaluations(CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        var evaluations = await _evaluationWorkflowService.GetMyEvaluationsAsync(userId, cancellationToken);
        return Ok(evaluations);
    }

    /// <summary>
    /// Get detailed evaluation information
    /// </summary>
    [HttpGet("{evaluationId}")]
    public async Task<IActionResult> GetEvaluationDetails(int evaluationId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var evaluation = await _evaluationWorkflowService.GetEvaluationDetailsAsync(evaluationId, userId, cancellationToken);
            return Ok(evaluation);
        }
        catch (Exception ex)
        {
            // Log the full exception for debugging
            var errorMessage = $"Error loading evaluation {evaluationId}: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $" | Inner: {ex.InnerException.Message}";
            }
            
            return StatusCode(500, new 
            { 
                error = "Failed to load evaluation details", 
                details = errorMessage,
                stackTrace = ex.StackTrace 
            });
        }
    }

    /// <summary>
    /// Approve an evaluation at the current stage
    /// </summary>
    [HttpPost("{evaluationId}/approve")]
    public async Task<IActionResult> ApproveEvaluation(
        int evaluationId,
        [FromBody] ApprovalActionDto dto,
        CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        await _evaluationWorkflowService.ApproveAsync(evaluationId, userId, dto.Comment, cancellationToken);
        return Ok(new { message = "Evaluation approved successfully." });
    }

    /// <summary>
    /// Reject an evaluation and return it to the employee
    /// </summary>
    [HttpPost("{evaluationId}/reject")]
    public async Task<IActionResult> RejectEvaluation(
        int evaluationId,
        [FromBody] ApprovalActionDto dto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Comment))
            return BadRequest(new { error = "A comment is required when rejecting an evaluation." });

        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        await _evaluationWorkflowService.RejectAsync(evaluationId, userId, dto.Comment, cancellationToken);
        return Ok(new { message = "Evaluation rejected successfully." });
    }

    /// <summary>
    /// Get available peer reviewers for an evaluation
    /// </summary>
    [HttpGet("{evaluationId}/available-peers")]
    public async Task<IActionResult> GetAvailablePeers(int evaluationId, CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        var peers = await _evaluationWorkflowService.GetAvailablePeersAsync(evaluationId, cancellationToken);
        return Ok(peers);
    }

    /// <summary>
    /// Assign peer reviewers
    /// </summary>
    [HttpPost("{evaluationId}/assign-peers")]
    public async Task<IActionResult> AssignPeerReviewers(
        int evaluationId,
        [FromBody] AssignPeersDto dto,
        CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        await _evaluationWorkflowService.AssignPeerReviewersAsync(
            evaluationId,
            userId,
            dto.PeerUserId1,
            dto.PeerUserId2,
            cancellationToken);
        
        return Ok(new { message = "Peer reviewers assigned successfully." });
    }

    /// <summary>
    /// Process promotion decision (GM only)
    /// </summary>
    [HttpPost("{evaluationId}/promotion-decision")]
    [Authorize(Roles = "GM")]
    public async Task<IActionResult> ProcessPromotionDecision(
        int evaluationId,
        [FromBody] PromotionDecisionDto dto,
        CancellationToken cancellationToken)
    {
        var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
        await _evaluationWorkflowService.ProcessPromotionDecisionAsync(
            evaluationId,
            userId,
            dto.Approve,
            dto.Comment,
            cancellationToken);
        
        var message = dto.Approve 
            ? "Promotion approved successfully. HR has been notified." 
            : "Promotion declined. Employee has been notified.";

        return Ok(new { message });
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

/// <summary>
/// DTO for promotion decision
/// </summary>
public class PromotionDecisionDto
{
    public bool Approve { get; set; }
    public string? Comment { get; set; }
}
