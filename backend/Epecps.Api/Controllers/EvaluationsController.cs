using Epecps.Application.DTOs.Evaluations;
using Epecps.Application.DTOs.Evaluations;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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
    private readonly IReviewScoringService _reviewScoringService;
    private readonly IUserSyncService _userSyncService;
    private readonly IEmailService _emailService;

    public EvaluationsController(
        IEvaluationWorkflowService evaluationWorkflowService,
        IReviewScoringService reviewScoringService,
        IUserSyncService userSyncService,
        IEmailService emailService)
    {
        _evaluationWorkflowService = evaluationWorkflowService;
        _reviewScoringService = reviewScoringService;
        _userSyncService = userSyncService;
        _emailService = emailService;
    }

    /// <summary>
    /// Get all pending approvals for the current user
    /// </summary>
    [HttpGet("pending-approvals")]
    public async Task<IActionResult> GetPendingApprovals(CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var pendingApprovals = await _evaluationWorkflowService.GetPendingApprovalsForUserAsync(userId, cancellationToken);
            return Ok(pendingApprovals);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while fetching pending approvals.", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all evaluations where current user is involved (pending + completed)
    /// </summary>
    [HttpGet("my-evaluations")]
    public async Task<IActionResult> GetMyEvaluations(CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var evaluations = await _evaluationWorkflowService.GetMyEvaluationsAsync(userId, cancellationToken);
            return Ok(evaluations);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while fetching evaluations.", details = ex.Message });
        }
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
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
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
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            await _evaluationWorkflowService.ApproveAsync(evaluationId, userId, dto.Comment, cancellationToken);
            return Ok(new { message = "Evaluation approved successfully." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error approving evaluation {evaluationId}: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $" | Inner: {ex.InnerException.Message}";
            }
            return StatusCode(500, new { error = "Failed to approve evaluation.", details = errorMessage });
        }
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
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Comment))
                return BadRequest(new { error = "A comment is required when rejecting an evaluation." });

            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            await _evaluationWorkflowService.RejectAsync(evaluationId, userId, dto.Comment, cancellationToken);
            return Ok(new { message = "Evaluation rejected successfully." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error rejecting evaluation {evaluationId}: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $" | Inner: {ex.InnerException.Message}";
            }
            return StatusCode(500, new { error = "Failed to reject evaluation.", details = errorMessage });
        }
    }

    /// <summary>
    /// Get available peer reviewers for an evaluation
    /// </summary>
    [HttpGet("{evaluationId}/available-peers")]
    public async Task<IActionResult> GetAvailablePeers(int evaluationId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var peers = await _evaluationWorkflowService.GetAvailablePeersAsync(evaluationId, cancellationToken);
            return Ok(peers);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to get available peers.", details = ex.Message });
        }
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
        try
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
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to assign peer reviewers.", details = ex.Message });
        }
    }

    /// <summary>
    /// Team Lead combined submission: overall score + assign peers in one action.
    /// </summary>
    [HttpPost("{evaluationId}/tl/combined-submit")]
    public async Task<IActionResult> SubmitTlCombinedReview(
        int evaluationId,
        [FromBody] SubmitTlCombinedReviewDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);

            await _evaluationWorkflowService.SubmitTlOverallAndAssignPeersAsync(
                evaluationId,
                userId,
                dto.OverallScore,
                dto.Comment,
                dto.PeerUserId1,
                dto.PeerUserId2,
                cancellationToken);

            return Ok(new { message = "TL review submitted and peer reviewers assigned successfully." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to submit TL combined review.", details = ex.Message });
        }
    }

    /// <summary>
    /// Process promotion decision (GM only)
    /// </summary>
    [HttpPost("{evaluationId}/promotion-decision")]
    public async Task<IActionResult> ProcessPromotionDecision(
        int evaluationId,
        [FromBody] PromotionDecisionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            
            // Check if user has GM role in database
            if (!await UserHasRoleAsync(userId, "GM", cancellationToken))
            {
                return StatusCode(403, new { error = "You must have the GM role to perform this action." });
            }
            
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
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to process promotion decision.", details = ex.Message });
        }
    }

    /// <summary>
    /// HOD recommends employee for promotion
    /// </summary>
    [HttpPost("{evaluationId}/hod/recommend")]
    public async Task<IActionResult> HodRecommendPromotion(
        int evaluationId,
        [FromBody] ApprovalActionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            
            // Check if user has HOD role in database
            if (!await UserHasRoleAsync(userId, "HOD", cancellationToken))
            {
                return StatusCode(403, new { error = "You must have the HOD role to perform this action." });
            }
            
            await _evaluationWorkflowService.RecommendForPromotionAsync(
                evaluationId,
                userId,
                dto.Comment,
                cancellationToken);

            return Ok(new { message = "Employee recommended for promotion successfully. GM has been notified." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to recommend for promotion.", details = ex.Message });
        }
    }

    /// <summary>
    /// HOD rejects evaluation
    /// </summary>
    [HttpPost("{evaluationId}/hod/reject")]
    public async Task<IActionResult> HodRejectEvaluation(
        int evaluationId,
        [FromBody] ApprovalActionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Comment))
                return BadRequest(new { error = "A comment is required when rejecting an evaluation at HOD stage." });

            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            
            // Check if user has HOD role in database
            if (!await UserHasRoleAsync(userId, "HOD", cancellationToken))
            {
                return StatusCode(403, new { error = "You must have the HOD role to perform this action." });
            }

            await _evaluationWorkflowService.RejectAtHodAsync(
                evaluationId,
                userId,
                dto.Comment,
                cancellationToken);

            return Ok(new { message = "Evaluation rejected at HOD stage. Employee has been notified." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to reject evaluation at HOD stage.", details = ex.Message });
        }
    }

    /// <summary>
    /// HR processes final promotion (after GM approval)
    /// </summary>
    [HttpPost("{evaluationId}/hr/process")]
    public async Task<IActionResult> HrProcessPromotion(
        int evaluationId,
        [FromBody] HrProcessDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            
            // Check if user has HR role in database
            if (!await UserHasRoleAsync(userId, "HR", cancellationToken))
            {
                return StatusCode(403, new { error = "You must have the HR role to perform this action." });
            }
            
            await _evaluationWorkflowService.FinalizePromotionByHrAsync(
                evaluationId,
                userId,
                dto.Proceed,
                dto.Comment,
                cancellationToken);

            var message = dto.Proceed
                ? "Promotion processed successfully. Employee has been notified."
                : "Promotion processing declined.";

            return Ok(new { message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to process promotion by HR.", details = ex.Message });
        }
    }

    /// <summary>
    /// TEST ENDPOINT: Send a test email to verify email configuration
    /// </summary>
    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail(CancellationToken cancellationToken)
    {
        try
        {
            var testEmail = "dilrukshadev@gmail.com";
            var testSubject = "EPECPS Email Test - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var testBody = @"
                <h1 style='color: #667eea;'>?? Email System Works!</h1>
                <p>If you receive this email, your EPECPS email configuration is correct.</p>
                <hr>
                <p><strong>Configuration Details:</strong></p>
                <ul>
                    <li>SMTP Server: smtp.gmail.com</li>
                    <li>Port: 587</li>
                    <li>Sender: dilrukshadev@gmail.com</li>
                    <li>Test Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"</li>
                </ul>
                <p style='color: #666; font-size: 12px;'>This is a test email from EPECPS.</p>
            ";

            await _emailService.SendEmailAsync(
                testEmail,
                "Test Recipient",
                testSubject,
                testBody,
                cancellationToken);
            
            return Ok(new 
            { 
                success = true,
                message = "Test email sent successfully! Check your inbox at " + testEmail,
                details = "If you don't receive the email within 2 minutes, check:\n" +
                         "1. Spam folder\n" +
                         "2. Console logs for errors\n" +
                         "3. Gmail app password is correct"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new 
            { 
                success = false,
                error = "Failed to send test email",
                details = ex.Message,
                innerError = ex.InnerException?.Message,
                suggestion = "Check:\n" +
                            "1. appsettings.json has EmailSettings section\n" +
                            "2. Gmail app password is correct: mshr utli ilwi kkmn\n" +
                            "3. API has been restarted after config changes"
            });
        }
    }

    /// <summary>
    /// Get email queue status for debugging
    /// </summary>
    [HttpGet("email-status")]
    public IActionResult GetEmailStatus()
    {
        try
        {
            return Ok(new 
            { 
                success = true,
                emailServiceConfigured = _emailService != null,
                message = "Email service is running and configured",
                note = "Submit a goal set or approve an evaluation to test email sending",
                checkLogs = "Watch the console for 'Email queued for...' and 'Email sent successfully' messages"
            });
        }
        catch (Exception ex)
        {
            return Ok(new 
            { 
                success = false,
                error = ex.Message,
                note = "Email service might not be properly configured"
            });
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

    /// <summary>
    /// Helper method to check if user has a specific role in the database
    /// </summary>
    private async Task<bool> UserHasRoleAsync(int userId, string roleName, CancellationToken cancellationToken = default)
    {
        // Use the UserSyncService to get user roles from database
        // We need to add a method to IUserSyncService to get roles
        // For now, we'll use direct database access
        var dbContext = HttpContext.RequestServices.GetRequiredService<Epecps.Infrastructure.Persistence.EpecpsDbContext>();
        
        var hasRole = await dbContext.Set<Epecps.Domain.Entities.UserRole>()
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == roleName, cancellationToken);
        
        return hasRole;
    }

    /// <summary>
    /// Submit RM review scores (item-level scoring for each goal)
    /// RM scores each goal individually on a 1-10 scale
    /// </summary>
    [HttpPost("{evaluationId}/reviews/{reviewId}/rm-scores")]
    public async Task<IActionResult> SubmitRmReviewScoring(
        int evaluationId,
        int reviewId,
        [FromBody] SubmitRmReviewScoringDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var result = await _reviewScoringService.SubmitRmReviewScoringAsync(
                evaluationId,
                reviewId,
                userId,
                dto,
                cancellationToken);

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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to submit RM scores.", details = ex.Message });
        }
    }

    /// <summary>
    /// Submit overall review score (TL/Peer/HOD/GM)
    /// These reviewers provide a single overall score (1-10) for the entire evaluation
    /// </summary>
    [HttpPost("{evaluationId}/reviews/{reviewId}/overall-score")]
    public async Task<IActionResult> SubmitOverallReviewScoring(
        int evaluationId,
        int reviewId,
        [FromBody] SubmitOverallReviewScoringDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var result = await _reviewScoringService.SubmitOverallReviewScoringAsync(
                evaluationId,
                reviewId,
                userId,
                dto,
                cancellationToken);

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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to submit overall score.", details = ex.Message });
        }
    }

    /// <summary>
    /// Submit per-goal scores for any reviewer role (TL/Peer/HOD).
    /// Enables individual goal scoring for all reviewer roles, not just RM.
    /// Each goal is scored on a 1-10 scale. An overall score is computed as the average.
    /// </summary>
    [HttpPost("{evaluationId}/reviews/{reviewId}/goal-scores")]
    public async Task<IActionResult> SubmitReviewWithGoalScores(
        int evaluationId,
        int reviewId,
        [FromBody] SubmitReviewWithGoalScoresDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var result = await _reviewScoringService.SubmitReviewWithGoalScoresAsync(
                evaluationId,
                reviewId,
                userId,
                dto,
                cancellationToken);

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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to submit goal scores.", details = ex.Message });
        }
    }

    // ========== NEW: Bulk Approval Endpoints ==========

    /// <summary>
    /// Get bulk approval statistics for GM/HR dashboard
    /// </summary>
    [HttpGet("bulk-approval/stats")]
    public async Task<IActionResult> GetBulkApprovalStats(CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var stats = await _evaluationWorkflowService.GetBulkApprovalStatsAsync(userId, cancellationToken);
            return Ok(stats);
        }
        catch (BusinessRuleException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to get bulk approval stats.", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all evaluations pending GM approval (for bulk approval)
    /// </summary>
    [HttpGet("bulk-approval/gm-pending")]
    public async Task<IActionResult> GetPendingGmBulkApprovals(CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var evaluations = await _evaluationWorkflowService.GetPendingGmBulkApprovalsAsync(userId, cancellationToken);
            return Ok(evaluations);
        }
        catch (BusinessRuleException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to get pending GM approvals.", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all evaluations pending HR processing (for bulk processing)
    /// </summary>
    [HttpGet("bulk-approval/hr-pending")]
    public async Task<IActionResult> GetPendingHrBulkProcessing(CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            var evaluations = await _evaluationWorkflowService.GetPendingHrBulkProcessingAsync(userId, cancellationToken);
            return Ok(evaluations);
        }
        catch (BusinessRuleException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to get pending HR processing.", details = ex.Message });
        }
    }

    /// <summary>
    /// GM bulk approves multiple evaluations at once
    /// </summary>
    [HttpPost("bulk-approval/gm-approve")]
    public async Task<IActionResult> GmBulkApprove(
        [FromBody] BulkApprovalRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            
            if (!await UserHasRoleAsync(userId, "GM", cancellationToken))
            {
                return StatusCode(403, new { error = "You must have the GM role to perform this action." });
            }

            var result = await _evaluationWorkflowService.GmBulkApproveAsync(userId, request, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to process bulk approval.", details = ex.Message });
        }
    }

    /// <summary>
    /// HR bulk processes multiple promotions at once
    /// </summary>
    [HttpPost("bulk-approval/hr-process")]
    public async Task<IActionResult> HrBulkProcess(
        [FromBody] BulkApprovalRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            
            if (!await UserHasRoleAsync(userId, "HR", cancellationToken))
            {
                return StatusCode(403, new { error = "You must have the HR role to perform this action." });
            }

            var result = await _evaluationWorkflowService.HrBulkProcessAsync(userId, request, cancellationToken);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to process bulk HR processing.", details = ex.Message });
        }
    }

    /// <summary>
    /// HOD submits overall score for an evaluation
    /// If score >= 8.5 (85%), routes to GM
    /// If score < 8.5 (85%), routes directly to HR
    /// </summary>
    [HttpPost("{evaluationId}/hod/submit-score")]
    public async Task<IActionResult> HodSubmitScore(
        int evaluationId,
        [FromBody] HodScoreSubmissionDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            
            if (!await UserHasRoleAsync(userId, "HOD", cancellationToken))
            {
                return StatusCode(403, new { error = "You must have the HOD role to perform this action." });
            }

            await _evaluationWorkflowService.HodSubmitScoreAsync(
                evaluationId,
                userId,
                dto.Score,
                dto.Comment,
                cancellationToken);

            var message = dto.Score >= 8.5m
                ? "Score submitted successfully. Employee has been routed to GM review."
                : "Score submitted successfully. Evaluation has been routed to HR processing.";

            return Ok(new { message, score = dto.Score, scorePercentage = dto.Score * 10 });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to submit HOD score.", details = ex.Message });
        }
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

/// <summary>
/// DTO for HR promotion processing
/// </summary>
public class HrProcessDto
{
    public bool Proceed { get; set; }
    public string? Comment { get; set; }
}

/// <summary>
/// DTO for HOD score submission
/// </summary>
public class HodScoreSubmissionDto
{
    public decimal Score { get; set; }
    public string? Comment { get; set; }
}
