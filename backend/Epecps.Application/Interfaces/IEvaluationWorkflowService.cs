using Epecps.Application.DTOs.Evaluations;
using Epecps.Domain.Entities;

namespace Epecps.Application.Interfaces;

/// <summary>
/// Service for managing the evaluation approval workflow
/// </summary>
public interface IEvaluationWorkflowService
{
    /// <summary>
    /// Start a new evaluation for a submitted goal set
    /// Creates evaluation record, self-review, initial RM review, and approval history
    /// </summary>
    /// <param name="employeeId">The employee submitting the goals</param>
    /// <param name="goalSetId">The goal set ID to evaluate</param>
    /// <param name="cycleId">The evaluation cycle ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created evaluation</returns>
    Task<Evaluation> StartEvaluationForGoalSetAsync(int employeeId, Guid goalSetId, int cycleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve the current stage of the evaluation and move to the next stage
    /// </summary>
    /// <param name="evaluationId">The evaluation ID</param>
    /// <param name="actorUserId">The user performing the approval</param>
    /// <param name="comment">Optional comment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ApproveAsync(int evaluationId, int actorUserId, string? comment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reject the evaluation and return it to the employee
    /// </summary>
    /// <param name="evaluationId">The evaluation ID</param>
    /// <param name="actorUserId">The user performing the rejection</param>
    /// <param name="comment">Required comment explaining the rejection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RejectAsync(int evaluationId, int actorUserId, string comment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assign peer reviewers (Team Lead only, during TL review stage)
    /// </summary>
    /// <param name="evaluationId">The evaluation ID</param>
    /// <param name="teamLeadUserId">The team lead user ID</param>
    /// <param name="peerUserId1">First peer reviewer</param>
    /// <param name="peerUserId2">Second peer reviewer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AssignPeerReviewersAsync(int evaluationId, int teamLeadUserId, int peerUserId1, int peerUserId2, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all pending approvals for a specific user based on their roles
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending approvals</returns>
    Task<IEnumerable<PendingApprovalDto>> GetPendingApprovalsForUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed evaluation information including reviews, goals, and approval history
    /// </summary>
    /// <param name="evaluationId">The evaluation ID</param>
    /// <param name="userId">The requesting user ID (for authorization)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed evaluation DTO</returns>
    Task<EvaluationDetailDto> GetEvaluationDetailsAsync(int evaluationId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve or reject a promotion case (GM only)
    /// </summary>
    /// <param name="evaluationId">The evaluation ID</param>
    /// <param name="gmUserId">The GM user ID</param>
    /// <param name="approve">True to approve, false to reject</param>
    /// <param name="comment">Optional comment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ProcessPromotionDecisionAsync(int evaluationId, int gmUserId, bool approve, string? comment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available peer reviewers for an evaluation
    /// </summary>
    /// <param name="evaluationId">The evaluation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available peer reviewers</returns>
    Task<IEnumerable<AvailablePeerDto>> GetAvailablePeersAsync(int evaluationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all evaluations where the user is involved (as employee, reviewer, or approver)
    /// Includes both pending and completed evaluations
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of evaluations where user is involved</returns>
    Task<IEnumerable<MyEvaluationDto>> GetMyEvaluationsAsync(int userId, CancellationToken cancellationToken = default);
}
