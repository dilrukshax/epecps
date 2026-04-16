using System.Security.Claims;
using Epecps.Application.DTOs.WorkflowV2;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Epecps.Api.Controllers;

[ApiController]
[Route("api/v2/workflow")]
[Authorize]
public class WorkflowV2Controller : ControllerBase
{
    private readonly IWorkflowV2Service _workflowV2Service;
    private readonly IUserSyncService _userSyncService;

    public WorkflowV2Controller(IWorkflowV2Service workflowV2Service, IUserSyncService userSyncService)
    {
        _workflowV2Service = workflowV2Service;
        _userSyncService = userSyncService;
    }

    [HttpGet("review-weights")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetReviewWeights(CancellationToken cancellationToken)
    {
        try
        {
            var weights = await _workflowV2Service.GetReviewWeightsAsync(cancellationToken);
            return Ok(weights);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to load review weights.", details = ex.Message });
        }
    }

    [HttpPut("review-weights")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateReviewWeights(
        [FromBody] UpdateWorkflowReviewWeightsDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _workflowV2Service.UpdateReviewWeightsAsync(request, cancellationToken);
            return Ok(updated);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to update review weights.", details = ex.Message });
        }
    }

    [HttpPost("goal-sets/{goalSetId:guid}/activation")]
    public async Task<IActionResult> SubmitActivationPlan(
        Guid goalSetId,
        [FromBody] SubmitActivationPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            await _workflowV2Service.SubmitActivationPlanAsync(goalSetId, userId, request, cancellationToken);
            return Ok(new { message = "Activation plan submitted successfully." });
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
            return StatusCode(500, new { error = "Failed to submit activation plan.", details = ex.Message });
        }
    }

    [HttpPost("evaluations/{evaluationId:int}/activation/decision")]
    [Authorize(Roles = "RM,SuperAdmin")]
    public async Task<IActionResult> ProcessActivationDecision(
        int evaluationId,
        [FromBody] ActivationPlanDecisionDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            await _workflowV2Service.ProcessActivationDecisionAsync(evaluationId, userId, request, cancellationToken);
            return Ok(new { message = request.Approved ? "Activation plan approved." : "Activation plan returned to employee." });
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
            return StatusCode(500, new { error = "Failed to process activation decision.", details = ex.Message });
        }
    }

    [HttpPost("evaluations/{evaluationId:int}/self-evaluation")]
    public async Task<IActionResult> SubmitSelfEvaluation(
        int evaluationId,
        [FromBody] SubmitSelfEvaluationV2Dto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            await _workflowV2Service.SubmitSelfEvaluationAsync(evaluationId, userId, request, cancellationToken);
            return Ok(new { message = "Self-evaluation submitted and parallel reviews started." });
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
            return StatusCode(500, new { error = "Failed to submit self-evaluation.", details = ex.Message });
        }
    }

    [HttpPost("evaluations/{evaluationId:int}/hod/finalize")]
    [Authorize(Roles = "HOD,SuperAdmin")]
    public async Task<IActionResult> HodFinalize(
        int evaluationId,
        [FromBody] HodFinalizeRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            await _workflowV2Service.HODFinalizeAsync(evaluationId, userId, request.Comment, cancellationToken);
            return Ok(new { message = "HOD finalization completed." });
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
            return StatusCode(500, new { error = "Failed to finalize HOD decision.", details = ex.Message });
        }
    }

    [HttpPost("evaluations/{evaluationId:int}/gm/decision")]
    [Authorize(Roles = "GM,SuperAdmin")]
    public async Task<IActionResult> GmDecision(
        int evaluationId,
        [FromBody] GmV2DecisionDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = await GetAuthenticatedUserIdAsync(cancellationToken);
            await _workflowV2Service.GmDecisionAsync(evaluationId, userId, request, cancellationToken);
            return Ok(new { message = "GM decision recorded.", request.VacancyAvailable });
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
            return StatusCode(500, new { error = "Failed to process GM decision.", details = ex.Message });
        }
    }

    [HttpGet("pip-cases")]
    [Authorize(Roles = "HR,SuperAdmin")]
    public async Task<IActionResult> GetPipCases(
        [FromQuery] int? assignedHrUserId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        try
        {
            var cases = await _workflowV2Service.GetPipCasesAsync(assignedHrUserId, status, cancellationToken);
            return Ok(cases);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to load PIP cases.", details = ex.Message });
        }
    }

    [HttpPatch("pip-cases/{pipCaseId:int}")]
    [Authorize(Roles = "HR,SuperAdmin")]
    public async Task<IActionResult> UpdatePipCase(
        int pipCaseId,
        [FromBody] PipCaseUpdateDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _workflowV2Service.UpdatePipCaseAsync(pipCaseId, request, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to update PIP case.", details = ex.Message });
        }
    }

    [HttpPost("pip-cases/{pipCaseId:int}/action-items")]
    [Authorize(Roles = "HR,SuperAdmin")]
    public async Task<IActionResult> AddPipActionItem(
        int pipCaseId,
        [FromBody] PipActionItemCreateDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _workflowV2Service.AddPipActionItemAsync(pipCaseId, request, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to add PIP action item.", details = ex.Message });
        }
    }

    [HttpPatch("pip-action-items/{pipActionItemId:int}")]
    [Authorize(Roles = "HR,SuperAdmin")]
    public async Task<IActionResult> UpdatePipActionItem(
        int pipActionItemId,
        [FromBody] PipActionItemUpdateDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _workflowV2Service.UpdatePipActionItemAsync(pipActionItemId, request, cancellationToken);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to update PIP action item.", details = ex.Message });
        }
    }

    private async Task<int> GetAuthenticatedUserIdAsync(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst("userId")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out var parsedUserId))
        {
            return parsedUserId;
        }

        var email = User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value
            ?? User.FindFirst("upn")?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(email))
        {
            var availableClaims = string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"));
            throw new UnauthorizedAccessException($"User email not found in authentication token. Available claims: {availableClaims}");
        }

        var fullName = User.FindFirst("name")?.Value
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst(ClaimTypes.GivenName)?.Value
            ?? email.Split('@')[0];

        return await _userSyncService.SyncUserFromClaimsAsync(email, fullName, cancellationToken);
    }
}
