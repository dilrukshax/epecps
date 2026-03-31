using Epecps.Application.DTOs.Evaluations;
using Epecps.Application.DTOs.WorkflowV2;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Domain.Enums;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// Implementation of the evaluation approval workflow service
/// Manages the approval matrix: Self ? RM ? Employee Start/Complete ? TL ? Peer1 ? Peer2 ? HOD ? GM ? HR
/// </summary>
public class EvaluationWorkflowService : IEvaluationWorkflowService
{
    private readonly EpecpsDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IWorkflowV2Service _workflowV2Service;

    // Evaluation status constants
    private const string STATUS_PENDING_RM_REVIEW = "Pending_RM_Review";
    private const string STATUS_APPROVED_BY_RM = "Approved_By_RM";
    private const string STATUS_RETURNED_TO_EMPLOYEE = "Returned_To_Employee";
    private const string STATUS_PENDING_EMPLOYEE_COMPLETION = "Pending_Employee_Completion";
    private const string STATUS_PENDING_TL_REVIEW = "Pending_TL_Review";
    private const string STATUS_PENDING_PEER_ASSIGNMENT = "Pending_Peer_Assignment";
    private const string STATUS_PENDING_PEER_REVIEWS = "Pending_Peer_Reviews";
    private const string STATUS_PENDING_HOD_REVIEW = "Pending_HOD_Review";
    private const string STATUS_PENDING_GM_DECISION = "Pending_GM_Decision";
    private const string STATUS_PENDING_HR_PROCESSING = "Pending_HR_Processing";
    private const string STATUS_COMPLETED_NO_PROMOTION = "Completed_NoPromotion";
    private const string STATUS_COMPLETED_PROMOTION_APPROVED = "Completed_PromotionApproved";
    private const string STATUS_COMPLETED_PROMOTION_REJECTED = "Completed_PromotionRejected";
    private const string STATUS_REJECTED = "Rejected";
    private const string STATUS_V2_PENDING_EMPLOYEE_ACTIVATION = "V2_PENDING_EMPLOYEE_ACTIVATION";
    private const string STATUS_V2_RETURNED_FOR_ACTIVATION = "V2_RETURNED_FOR_ACTIVATION";
    private const string STATUS_V2_PENDING_TL_ACTIVATION_REVIEW = "V2_PENDING_TL_ACTIVATION_REVIEW";
    private const string STATUS_V2_ACTIVE_GOALS = "V2_ACTIVE_GOALS";
    private const string STATUS_V2_PENDING_PARALLEL_REVIEWS = "V2_PENDING_PARALLEL_REVIEWS";
    private const string STATUS_V2_PENDING_HOD_REVIEW = "V2_PENDING_HOD_REVIEW";
    private const string STATUS_V2_PENDING_GM_DECISION = "V2_PENDING_GM_DECISION";
    private const string STATUS_V2_PENDING_HR_PROMOTION = "V2_PENDING_HR_PROMOTION";
    private const string STATUS_V2_PENDING_HR_LOW_PERFORMER = "V2_PENDING_HR_LOW_PERFORMER";
    private const string STATUS_V2_COMPLETED_PROMOTION_APPROVED = "V2_COMPLETED_PROMOTION_APPROVED";
    private const string STATUS_V2_COMPLETED_NO_PROMOTION = "V2_COMPLETED_NO_PROMOTION";

    // Review status constants
    private const string REVIEW_STATUS_PENDING = "Pending";
    private const string REVIEW_STATUS_APPROVED = "Approved";
    private const string REVIEW_STATUS_REJECTED = "Rejected";
    private const string REVIEW_STATUS_COMPLETED = "Completed";

    // Score threshold for promotion
    private const decimal PROMOTION_THRESHOLD = 80.0m;

    public EvaluationWorkflowService(
        EpecpsDbContext context,
        IEmailService emailService,
        IWorkflowV2Service workflowV2Service)
    {
        _context = context;
        _emailService = emailService;
        _workflowV2Service = workflowV2Service;
    }

    public async Task<Evaluation> StartEvaluationForGoalSetAsync(int employeeId, Guid goalSetId, int cycleId, CancellationToken cancellationToken = default)
    {
        // Get employee details
        var employee = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == employeeId, cancellationToken);

        if (employee == null)
            throw new NotFoundException(nameof(User), employeeId);

        // Get cycle details
        var cycle = await _context.Set<Cycle>()
            .FirstOrDefaultAsync(c => c.CycleId == cycleId, cancellationToken);

        if (cycle == null)
            throw new NotFoundException(nameof(Cycle), cycleId);

        // Get all personal goals in the goal set
        var personalGoals = await _context.PersonalGoals
            .Where(pg => pg.GoalSetId == goalSetId && pg.UserId == employeeId)
            .ToListAsync(cancellationToken);

        if (!personalGoals.Any())
            throw new NotFoundException("No goals found in the specified goal set.");

        // Check if there's already an active evaluation for this goal set
        var existingEvaluation = await _context.Set<Evaluation>()
            .Where(e => e.GoalSetId == goalSetId && e.EmployeeId == employeeId)
            .Where(e => !e.Status.Contains("Completed") && e.Status != STATUS_REJECTED && e.Status != STATUS_RETURNED_TO_EMPLOYEE)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingEvaluation != null)
            throw new BusinessRuleException($"An active evaluation already exists for this goal set (Status: {existingEvaluation.Status}). Please complete or cancel the existing evaluation first.");

        // Determine RM and TL from organizational structure
        var reportingManagerId = await GetReportingManagerIdAsync(employeeId, cancellationToken);
        var teamLeadId = await GetTeamLeadIdAsync(employeeId, cancellationToken);

        // Create the evaluation with PENDING_RM_REVIEW status
        var evaluation = new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = reportingManagerId,
            TeamLeadId = teamLeadId,
            GoalSetId = goalSetId,
            Status = STATUS_PENDING_RM_REVIEW,
            OverallScore = null
        };

        _context.Set<Evaluation>().Add(evaluation);
        await _context.SaveChangesAsync(cancellationToken);

        // Link personal goals to evaluation via EmployeeGoals
        foreach (var personalGoal in personalGoals)
        {
            var employeeGoal = new EmployeeGoal
            {
                EvaluationId = evaluation.EvaluationId,
                PersonalGoalId = personalGoal.Id, // ? Set the PersonalGoalId for proper mapping
                Title = personalGoal.Title,
                Description = personalGoal.Description ?? string.Empty,
                WeightPct = 100m / personalGoals.Count,
                EvidenceUri = null
            };

            _context.Set<EmployeeGoal>().Add(employeeGoal);
            
            // Update personal goal status to PendingRMReview
            personalGoal.Status = PersonalGoalStatus.PendingRMReview;
            personalGoal.UpdatedAt = DateTime.UtcNow;
        }

        // Create self-review (already completed)
        var selfReview = new Review
        {
            EvaluationId = evaluation.EvaluationId,
            ReviewerUserId = employeeId,
            ReviewerRole = ReviewerRole.Self,
            Status = REVIEW_STATUS_COMPLETED,
            OverallComment = "Self-evaluation completed based on personal goals.",
            SubmittedAt = DateTime.UtcNow
        };

        _context.Set<Review>().Add(selfReview);

        // Create RM review (pending)
        var rmReview = new Review
        {
            EvaluationId = evaluation.EvaluationId,
            ReviewerUserId = reportingManagerId,
            ReviewerRole = ReviewerRole.RM,
            Status = REVIEW_STATUS_PENDING,
            OverallComment = null,
            SubmittedAt = null
        };

        _context.Set<Review>().Add(rmReview);

        await _context.SaveChangesAsync(cancellationToken);

        // Create approval history entry
        var approvalHistory = new ApprovalHistory
        {
            EvaluationId = evaluation.EvaluationId,
            ReviewId = selfReview.ReviewId,
            ActorUserId = employeeId,
            ActorRole = "Employee",
            Action = "SubmittedToRM",
            Comment = "Goal set submitted for RM review",
            FromStatus = "Draft",
            ToStatus = STATUS_PENDING_RM_REVIEW,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ApprovalHistory>().Add(approvalHistory);

        // Create notification for RM
        var notification = new Notification
        {
            UserId = reportingManagerId,
            Subject = $"New Goal Set Pending Review: {employee.FullName}",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        };

        _context.Set<Notification>().Add(notification);

        // Create audit log
        var auditLog = new AuditLog
        {
            ActorUserId = employeeId,
            EntityType = "Evaluation",
            EntityId = evaluation.EvaluationId,
            Action = "GOAL_SET_SUBMITTED_TO_RM",
            BeforeJson = null,
            AfterJson = System.Text.Json.JsonSerializer.Serialize(new { evaluation.EvaluationId, evaluation.Status }),
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<AuditLog>().Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        // Send email to Reporting Manager
        var reportingManager = await _context.Users.FindAsync(new object[] { reportingManagerId }, cancellationToken);
        if (reportingManager != null)
        {
            await _emailService.SendEvaluationNotificationAsync(
                reportingManager.Email,
                reportingManager.FullName,
                employee.FullName,
                "Submitted",
                "RM",
                "Employee has submitted their goal set for review. Please review and approve or return for revision.",
                evaluation.EvaluationId,
                cancellationToken);
        }

        return evaluation;
    }

    public async Task ApproveAsync(int evaluationId, int actorUserId, string? comment, CancellationToken cancellationToken = default)
    {
        // ? FIX: Reload evaluation with fresh data from database each time to avoid stale data
        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.Reviews)
            .Include(e => e.PeerAssignments)
            .Include(e => e.Employee)
            .AsNoTracking() // Use AsNoTracking to ensure we get fresh data
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
            throw new NotFoundException(nameof(Evaluation), evaluationId);

        // ? FIX: Now re-attach to track changes
        evaluation = await _context.Set<Evaluation>()
            .Include(e => e.Reviews)
            .Include(e => e.PeerAssignments)
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
            throw new NotFoundException(nameof(Evaluation), evaluationId);

        // Get actor's roles
        var actorRoles = await GetUserRolesAsync(actorUserId, cancellationToken);
        var oldStatus = evaluation.Status;

        // Determine current review based on status
        Review? currentReview = null;
        string actorRole = string.Empty;
        string action = "Approved";

        switch (evaluation.Status)
        {
            case STATUS_PENDING_RM_REVIEW:
                if (actorUserId != evaluation.ReportingManagerId)
                    throw new BusinessRuleException("Only the Reporting Manager can approve at this stage.");
                
                currentReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.RM && r.Status == REVIEW_STATUS_PENDING);
                actorRole = "RM";
                action = "RMApproved";
                
                // RM approved - update status to APPROVED_BY_RM
                // Employee can now start working on goals
                evaluation.Status = STATUS_APPROVED_BY_RM;
                
                // Update personal goals status to ApprovedByRM
                var personalGoals = await _context.PersonalGoals
                    .Where(pg => pg.GoalSetId == evaluation.GoalSetId && pg.UserId == evaluation.EmployeeId)
                    .ToListAsync(cancellationToken);
                
                foreach (var goal in personalGoals)
                {
                    goal.Status = PersonalGoalStatus.ApprovedByRM;
                    goal.UpdatedAt = DateTime.UtcNow;
                }
                
                // Notify employee that they can start working on goals
                var employeeNotification = new Notification
                {
                    UserId = evaluation.EmployeeId,
                    Subject = "Goal Set Approved – Ready to Start!",
                    Channel = "Email",
                    SentAt = DateTime.UtcNow
                };
                _context.Set<Notification>().Add(employeeNotification);
                
                // Send email to employee
                if (evaluation.Employee != null)
                {
                    await _emailService.SendEvaluationNotificationAsync(
                        evaluation.Employee.Email,
                        evaluation.Employee.FullName,
                        evaluation.Employee.FullName,
                        "Approved",
                        "Employee",
                        "Your goal set has been approved by your Reporting Manager. You can now start working on your goals. Click 'Start' on each goal when you begin.",
                        evaluationId,
                        cancellationToken);
                }
                break;

            case "Pending_RM_Review_PostCompletion":
                // Second RM approval after employee completion
                if (actorUserId != evaluation.ReportingManagerId)
                    throw new BusinessRuleException("Only the Reporting Manager can approve at this stage.");
                
                // Find the post-completion RM review (the most recent pending one)
                currentReview = evaluation.Reviews
                    .Where(r => r.ReviewerRole == ReviewerRole.RM && r.Status == REVIEW_STATUS_PENDING)
                    .OrderByDescending(r => r.ReviewId)
                    .FirstOrDefault();
                
                actorRole = "RM";
                action = "RMApproved";
                
                // After second RM approval, proceed to TL review
                var tlReview = new Review
                {
                    EvaluationId = evaluation.EvaluationId,
                    ReviewerUserId = evaluation.TeamLeadId,
                    ReviewerRole = ReviewerRole.TL,
                    Status = REVIEW_STATUS_PENDING,
                    OverallComment = null,
                    SubmittedAt = null
                };

                _context.Set<Review>().Add(tlReview);
                evaluation.Status = STATUS_PENDING_TL_REVIEW;
                
                // Notify TL
                var tlNotification = new Notification
                {
                    UserId = evaluation.TeamLeadId,
                    Subject = $"Goals Completed - Evaluation Ready for Review: {evaluation.Employee?.FullName ?? "Employee"}",
                    Channel = "Email",
                    SentAt = DateTime.UtcNow
                };

                _context.Set<Notification>().Add(tlNotification);
                
                // Send email to TL
                var teamLead = await _context.Users.FindAsync(new object[] { evaluation.TeamLeadId }, cancellationToken);
                var employee = evaluation.Employee ?? await _context.Users.FindAsync(new object[] { evaluation.EmployeeId }, cancellationToken);
                var rm = await _context.Users.FindAsync(new object[] { evaluation.ReportingManagerId }, cancellationToken);
                
                if (teamLead != null && employee != null)
                {
                    await _emailService.SendApprovalNotificationAsync(
                        teamLead.Email,
                        teamLead.FullName,
                        employee.FullName,
                        rm?.FullName ?? "RM",
                        "RM",
                        "Employee has completed all goals and RM has approved. Please review the evaluation and assign peer reviewers.",
                        evaluationId,
                        cancellationToken);
                }
                break;

            case STATUS_PENDING_TL_REVIEW:
                if (actorUserId != evaluation.TeamLeadId)
                    throw new BusinessRuleException("Only the Team Lead can approve at this stage.");
                
                currentReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.TL);
                actorRole = "TL";
                // After TL approval, they must assign peers
                evaluation.Status = STATUS_PENDING_PEER_ASSIGNMENT;
                break;

            case STATUS_PENDING_PEER_REVIEWS:
                // Check if actor is one of the assigned peers
                var peerAssignment = evaluation.PeerAssignments.FirstOrDefault(pa => pa.PeerUserId == actorUserId);
                if (peerAssignment == null)
                    throw new BusinessRuleException("Only assigned peer reviewers can approve at this stage.");
                
                // ? FIX: Find the FIRST pending peer review for this user (allows same user to approve twice)
                currentReview = evaluation.Reviews
                    .Where(r => r.ReviewerRole == ReviewerRole.Peer && 
                               r.ReviewerUserId == actorUserId && 
                               r.Status == REVIEW_STATUS_PENDING)
                    .OrderBy(r => r.ReviewId)
                    .FirstOrDefault();
                
                if (currentReview == null)
                    throw new BusinessRuleException("You have already completed all your peer reviews for this evaluation.");
                
                actorRole = "Peer";
                break;

            case STATUS_PENDING_HOD_REVIEW:
                if (!actorRoles.Contains("HOD"))
                    throw new BusinessRuleException("Only HOD can approve at this stage.");
                
                currentReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.HOD);
                actorRole = "HOD";
                await TransitionAfterHodReviewAsync(evaluation, actorUserId, cancellationToken);
                break;

            default:
                throw new BusinessRuleException($"Evaluation cannot be approved in current status: {evaluation.Status}");
        }

        // Update the review
        if (currentReview != null)
        {
            currentReview.Status = REVIEW_STATUS_APPROVED;
            currentReview.OverallComment = comment ?? currentReview.OverallComment;
            currentReview.SubmittedAt = DateTime.UtcNow;
            
            // ? FIX: Explicitly mark the review as modified to ensure EF tracks the changes
            _context.Entry(currentReview).State = EntityState.Modified;
            
            // ? FIX: Save changes immediately to ensure review status is persisted
            // This prevents issues with subsequent checks and ensures atomicity
            await _context.SaveChangesAsync(cancellationToken);
        }
        
        // Check if we need to transition after peer reviews (must be done AFTER updating the review)
        if (evaluation.Status == STATUS_PENDING_PEER_REVIEWS)
        {
            await CheckAndTransitionAfterPeerReviewsAsync(evaluation, cancellationToken);
        }

        // Create approval history
        var approvalHistory = new ApprovalHistory
        {
            EvaluationId = evaluationId,
            ReviewId = currentReview?.ReviewId,
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            Action = action,
            Comment = comment,
            FromStatus = oldStatus,
            ToStatus = evaluation.Status,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ApprovalHistory>().Add(approvalHistory);

        // Create audit log
        var auditLog = new AuditLog
        {
            ActorUserId = actorUserId,
            EntityType = "Evaluation",
            EntityId = evaluationId,
            Action = $"EVALUATION_APPROVED_{actorRole}",
            BeforeJson = System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus }),
            AfterJson = System.Text.Json.JsonSerializer.Serialize(new { Status = evaluation.Status }),
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<AuditLog>().Add(auditLog);

        // Create notification for next approver or employee (skip for RM approval as we already notified)
        if (evaluation.Status != STATUS_APPROVED_BY_RM && evaluation.Status != "Pending_RM_Review_PostCompletion")
        {
            await CreateNextStepNotificationAsync(evaluation, actorUserId, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(int evaluationId, int actorUserId, string comment, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(comment))
            throw new BusinessRuleException("A comment is required when rejecting an evaluation.");

        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.Reviews)
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
            throw new NotFoundException(nameof(Evaluation), evaluationId);

        var actorRoles = await GetUserRolesAsync(actorUserId, cancellationToken);
        var oldStatus = evaluation.Status;

        // Determine current review and actor role
        Review? currentReview = null;
        string actorRole = string.Empty;
        string action = "Rejected";

        switch (evaluation.Status)
        {
            case STATUS_PENDING_RM_REVIEW:
                if (actorUserId != evaluation.ReportingManagerId)
                    throw new BusinessRuleException("Only the Reporting Manager can reject at this stage.");
                currentReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.RM);
                actorRole = "RM";
                action = "RMRejected";
                
                // RM rejected - return to employee for revision
                evaluation.Status = STATUS_RETURNED_TO_EMPLOYEE;
                
                // Update personal goals status to ReturnedToEmployee
                var personalGoals = await _context.PersonalGoals
                    .Where(pg => pg.GoalSetId == evaluation.GoalSetId && pg.UserId == evaluation.EmployeeId)
                    .ToListAsync(cancellationToken);
                
                foreach (var goal in personalGoals)
                {
                    goal.Status = PersonalGoalStatus.ReturnedToEmployee;
                    goal.UpdatedAt = DateTime.UtcNow;
                }
                break;

            case "Pending_RM_Review_PostCompletion":
                // Second RM rejection after employee completion
                if (actorUserId != evaluation.ReportingManagerId)
                    throw new BusinessRuleException("Only the Reporting Manager can reject at this stage.");
                
                currentReview = evaluation.Reviews
                    .Where(r => r.ReviewerRole == ReviewerRole.RM && r.Status == REVIEW_STATUS_PENDING)
                    .OrderByDescending(r => r.ReviewId)
                    .FirstOrDefault();
                
                actorRole = "RM";
                action = "RMRejected";
                
                // Return to employee to redo goals
                evaluation.Status = STATUS_RETURNED_TO_EMPLOYEE;
                
                // Update personal goals status to ReturnedToEmployee
                var goalsPostCompletion = await _context.PersonalGoals
                    .Where(pg => pg.GoalSetId == evaluation.GoalSetId && pg.UserId == evaluation.EmployeeId)
                    .ToListAsync(cancellationToken);
                
                foreach (var goal in goalsPostCompletion)
                {
                    goal.Status = PersonalGoalStatus.ReturnedToEmployee;
                    goal.UpdatedAt = DateTime.UtcNow;
                    // Reset completion timestamps
                    goal.StartedAt = null;
                    goal.CompletedAt = null;
                }
                break;

            case STATUS_PENDING_TL_REVIEW:
                if (actorUserId != evaluation.TeamLeadId)
                    throw new BusinessRuleException("Only the Team Lead can reject at this stage.");
                currentReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.TL);
                actorRole = "TL";
                evaluation.Status = STATUS_REJECTED;
                break;

            case STATUS_PENDING_PEER_REVIEWS:
                var peerAssignment = evaluation.PeerAssignments.FirstOrDefault(pa => pa.PeerUserId == actorUserId);
                if (peerAssignment == null)
                    throw new BusinessRuleException("Only assigned peer reviewers can reject at this stage.");
                currentReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.Peer && r.ReviewerUserId == actorUserId);
                actorRole = "Peer";
                evaluation.Status = STATUS_REJECTED;
                break;

            case STATUS_PENDING_HOD_REVIEW:
                if (!actorRoles.Contains("HOD"))
                    throw new BusinessRuleException("Only HOD can reject at this stage.");
                currentReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.HOD);
                actorRole = "HOD";
                evaluation.Status = STATUS_REJECTED;
                break;

            default:
                throw new BusinessRuleException($"Evaluation cannot be rejected in current status: {evaluation.Status}");
        }

        // Update review
        if (currentReview != null)
        {
            currentReview.Status = REVIEW_STATUS_REJECTED;
            currentReview.OverallComment = comment;
            currentReview.SubmittedAt = DateTime.UtcNow;
        }

        // If not returned to employee (i.e., fully rejected), unlock personal goals
        if (evaluation.Status == STATUS_REJECTED)
        {
            var rejectedGoals = await _context.PersonalGoals
                .Where(pg => pg.GoalSetId == evaluation.GoalSetId && pg.UserId == evaluation.EmployeeId)
                .ToListAsync(cancellationToken);

            foreach (var goal in rejectedGoals)
            {
                goal.Status = PersonalGoalStatus.Completed;
                goal.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Create approval history
        var approvalHistory = new ApprovalHistory
        {
            EvaluationId = evaluationId,
            ReviewId = currentReview?.ReviewId,
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            Action = action,
            Comment = comment,
            FromStatus = oldStatus,
            ToStatus = evaluation.Status,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ApprovalHistory>().Add(approvalHistory);

        // Notify employee
        var notification = new Notification
        {
            UserId = evaluation.EmployeeId,
            Subject = evaluation.Status == STATUS_RETURNED_TO_EMPLOYEE 
                ? $"Goal Set Returned for Revision by {actorRole}"
                : $"Evaluation Rejected by {actorRole}",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        };

        _context.Set<Notification>().Add(notification);

        // Send rejection email to Employee
        var actor = await _context.Users.FindAsync(new object[] { actorUserId }, cancellationToken);
        if (actor != null && evaluation.Employee != null)
        {
            var message = evaluation.Status == STATUS_RETURNED_TO_EMPLOYEE
                ? $"Your goal set has been returned for revision by your {actorRole}. Please review their feedback and make necessary changes before resubmitting."
                : $"Your evaluation has been rejected by {actorRole}.";
            
            await _emailService.SendRejectionNotificationAsync(
                evaluation.Employee.Email,
                evaluation.Employee.FullName,
                evaluation.Employee.FullName,
                actor.FullName,
                actorRole,
                comment,
                evaluationId,
                cancellationToken);
        }

        // Create audit log
        var auditLog = new AuditLog
        {
            ActorUserId = actorUserId,
            EntityType = "Evaluation",
            EntityId = evaluationId,
            Action = $"EVALUATION_REJECTED_{actorRole}",
            BeforeJson = System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus }),
            AfterJson = System.Text.Json.JsonSerializer.Serialize(new { Status = evaluation.Status, Comment = comment }),
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<AuditLog>().Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Evaluation> ContinueWorkflowAfterEmployeeCompletionAsync(int evaluationId, CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.Reviews)
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
            throw new NotFoundException(nameof(Evaluation), evaluationId);

        // Validate that evaluation is in the correct status
        if (evaluation.Status != STATUS_APPROVED_BY_RM && evaluation.Status != STATUS_PENDING_EMPLOYEE_COMPLETION)
            throw new BusinessRuleException($"Workflow can only continue when employee has completed all goals. Current status: {evaluation.Status}");

        var oldStatus = evaluation.Status;

        // Create a second RM review (for post-completion approval)
        var rmReviewPostCompletion = new Review
        {
            EvaluationId = evaluation.EvaluationId,
            ReviewerUserId = evaluation.ReportingManagerId,
            ReviewerRole = ReviewerRole.RM,
            Status = REVIEW_STATUS_PENDING,
            OverallComment = null,
            SubmittedAt = null
        };

        _context.Set<Review>().Add(rmReviewPostCompletion);

        // Update evaluation status to pending RM review (for the second approval after completion)
        evaluation.Status = "Pending_RM_Review_PostCompletion";

        // Create approval history
        var approvalHistory = new ApprovalHistory
        {
            EvaluationId = evaluationId,
            ReviewId = null,
            ActorUserId = evaluation.EmployeeId,
            ActorRole = "Employee",
            Action = "EmployeeCompletedAllGoals;WorkflowContinued",
            Comment = "All goals completed. Workflow advanced to RM for post-completion review.",
            FromStatus = oldStatus,
            ToStatus = evaluation.Status,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ApprovalHistory>().Add(approvalHistory);

        // Notify RM for second approval
        var rmNotification = new Notification
        {
            UserId = evaluation.ReportingManagerId,
            Subject = $"Goals Completed - Final Review Required: {evaluation.Employee?.FullName ?? "Employee"}",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        };

        _context.Set<Notification>().Add(rmNotification);

        // Create audit log
        var auditLog = new AuditLog
        {
            ActorUserId = evaluation.EmployeeId,
            EntityType = "Evaluation",
            EntityId = evaluationId,
            Action = "WORKFLOW_CONTINUED_TO_RM_POSTCOMPLETE",
            BeforeJson = System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus }),
            AfterJson = System.Text.Json.JsonSerializer.Serialize(new { Status = evaluation.Status }),
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<AuditLog>().Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        // Send email to Reporting Manager for second approval
        var rm = await _context.Users.FindAsync(new object[] { evaluation.ReportingManagerId }, cancellationToken);
        var employee = evaluation.Employee ?? await _context.Users.FindAsync(new object[] { evaluation.EmployeeId }, cancellationToken);
        
        if (rm != null && employee != null)
        {
            await _emailService.SendEvaluationNotificationAsync(
                rm.Email,
                rm.FullName,
                employee.FullName,
                "Pending",
                "RM",
                "Employee has completed all goals. Please review the completion and approve to proceed to Team Lead review.",
                evaluationId,
                cancellationToken);
        }

        return evaluation;
    }

    private async Task<int> GetReportingManagerIdAsync(int employeeId, CancellationToken cancellationToken)
    {
        // TODO: Implement actual organizational hierarchy lookup
        // For now, return a placeholder - first user with RM role
        var rmUser = await _context.Set<UserRole>()
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.Name == "RM")
            .Select(ur => ur.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (rmUser == 0)
            throw new BusinessRuleException("No Reporting Manager found in the system. Please configure organizational structure.");

        return rmUser;
    }

    private async Task<int> GetTeamLeadIdAsync(int employeeId, CancellationToken cancellationToken)
    {
        // TODO: Implement actual organizational hierarchy lookup
        // For now, return a placeholder - first user with TL role
        var tlUser = await _context.Set<UserRole>()
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.Name == "TL")
            .Select(ur => ur.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (tlUser == 0)
            throw new BusinessRuleException("No Team Lead found in the system. Please configure organizational structure.");

        return tlUser;
    }

    private async Task<List<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.Set<UserRole>()
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync(cancellationToken);
    }

    private async Task TransitionToTeamLeadReviewAsync(Evaluation evaluation, CancellationToken cancellationToken)
    {
        // Create TL review
        var tlReview = new Review
        {
            EvaluationId = evaluation.EvaluationId,
            ReviewerUserId = evaluation.TeamLeadId,
            ReviewerRole = ReviewerRole.TL,
            Status = REVIEW_STATUS_PENDING,
            OverallComment = null,
            SubmittedAt = null
        };

        _context.Set<Review>().Add(tlReview);

        // Update evaluation status
        evaluation.Status = STATUS_PENDING_TL_REVIEW;

        // Notify TL
        var notification = new Notification
        {
            UserId = evaluation.TeamLeadId,
            Subject = $"Evaluation Pending: {evaluation.Employee?.FullName ?? "Employee"}",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        };

        _context.Set<Notification>().Add(notification);

        // ?? SEND EMAIL to Team Lead
        var teamLead = await _context.Users.FindAsync(new object[] { evaluation.TeamLeadId }, cancellationToken);
        var employee = evaluation.Employee ?? await _context.Users.FindAsync(new object[] { evaluation.EmployeeId }, cancellationToken);
        var rm = evaluation.ReportingManager ?? await _context.Users.FindAsync(new object[] { evaluation.ReportingManagerId }, cancellationToken);
        
        if (teamLead != null && employee != null && rm != null)
        {
            await _emailService.SendApprovalNotificationAsync(
                teamLead.Email,
                teamLead.FullName,
                employee.FullName,
                rm.FullName,
                "RM",
                "Please review and approve the evaluation, then assign peer reviewers.",
                evaluation.EvaluationId,
                cancellationToken);
        }

        await Task.CompletedTask;
    }

    private async Task CheckAndTransitionAfterPeerReviewsAsync(Evaluation evaluation, CancellationToken cancellationToken)
    {
        // ? FIX: Reload evaluation to get fresh data from database
        var freshEvaluation = await _context.Set<Evaluation>()
            .Include(e => e.Reviews)
            .Include(e => e.Employee)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluation.EvaluationId, cancellationToken);

        if (freshEvaluation == null) return;

        // Check if both peer reviews are approved or completed
        var peerReviews = freshEvaluation.Reviews.Where(r => r.ReviewerRole == ReviewerRole.Peer).ToList();
        
        Console.WriteLine($"CheckAndTransitionAfterPeerReviews: Found {peerReviews.Count} peer reviews");
        foreach (var pr in peerReviews)
        {
            Console.WriteLine($"  Peer Review {pr.ReviewId}: Status={pr.Status}, ReviewerUserId={pr.ReviewerUserId}");
        }
        
        // ? FIX: Check for both "Approved" and "Completed" statuses
        var allPeerReviewsDone = peerReviews.Count == 2 && 
            peerReviews.All(r => r.Status == REVIEW_STATUS_APPROVED || r.Status == REVIEW_STATUS_COMPLETED);
        
        Console.WriteLine($"CheckAndTransitionAfterPeerReviews: allPeerReviewsDone={allPeerReviewsDone}");
        
        if (allPeerReviewsDone)
        {
            Console.WriteLine("CheckAndTransitionAfterPeerReviews: Both peers done, transitioning to HOD review");
            
            // Both peers approved, move to HOD review
            var hodReview = new Review
            {
                EvaluationId = evaluation.EvaluationId,
                ReviewerUserId = await GetHodUserIdAsync(cancellationToken),
                ReviewerRole = ReviewerRole.HOD,
                Status = REVIEW_STATUS_PENDING,
                OverallComment = null,
                SubmittedAt = null
            };

            _context.Set<Review>().Add(hodReview);

            evaluation.Status = STATUS_PENDING_HOD_REVIEW;

            // Notify HOD
            var notification = new Notification
            {
                UserId = hodReview.ReviewerUserId,
                Subject = $"Evaluation Pending: {evaluation.Employee?.FullName ?? freshEvaluation.Employee?.FullName ?? "Employee"}",
                Channel = "Email",
                SentAt = DateTime.UtcNow
            };

            _context.Set<Notification>().Add(notification);

            // ? SEND EMAIL to HOD
            var hod = await _context.Users.FindAsync(new object[] { hodReview.ReviewerUserId }, cancellationToken);
            var employee = evaluation.Employee ?? freshEvaluation.Employee ?? await _context.Users.FindAsync(new object[] { evaluation.EmployeeId }, cancellationToken);
            
            if (hod != null && employee != null)
            {
                await _emailService.SendEvaluationNotificationAsync(
                    hod.Email,
                    hod.FullName,
                    employee.FullName,
                    "Pending",
                    "HOD",
                    "All peer reviews are complete. Please review the evaluation and decide on promotion recommendation.",
                    evaluation.EvaluationId,
                    cancellationToken);
            }
            
            // ? Save changes immediately to ensure status is persisted
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            Console.WriteLine($"CheckAndTransitionAfterPeerReviews: Not all peers done yet. Waiting for remaining reviews.");
        }

        await Task.CompletedTask;
    }

    private async Task TransitionAfterHodReviewAsync(Evaluation evaluation, int hodUserId, CancellationToken cancellationToken)
    {
        // Calculate overall score: prefer per-goal scores averaged across reviewers and goals,
        // then fallback to overall-only scores, then ReviewItems
        var reviewIds = evaluation.Reviews.Select(r => r.ReviewId).ToList();

        // First try per-goal scores: average each goal across reviewers, then average across goals
        var perGoalScores = await _context.Set<ReviewScore>()
            .Include(rs => rs.Review)
            .Where(rs => reviewIds.Contains(rs.ReviewId)
                && rs.PersonalGoalId != null
                && (rs.Review.Status == "Completed" || rs.Review.Status == "Approved"))
            .GroupBy(rs => rs.PersonalGoalId)
            .Select(g => g.Average(rs => rs.ScoreValue))
            .ToListAsync(cancellationToken);

        if (perGoalScores.Count > 0)
        {
            // Per-goal scores are 1-10, convert average to percentage
            evaluation.OverallScore = Math.Round(perGoalScores.Average() * 10m, 2);
        }
        else
        {
            // Fallback to overall-only scores (PersonalGoalId == null)
            var overallScores = await _context.Set<ReviewScore>()
                .Include(rs => rs.Review)
                .Where(rs => reviewIds.Contains(rs.ReviewId)
                    && rs.PersonalGoalId == null
                    && (rs.Review.Status == "Completed" || rs.Review.Status == "Approved"))
                .Select(rs => rs.ScoreValue)
                .ToListAsync(cancellationToken);

            if (overallScores.Any())
            {
                evaluation.OverallScore = Math.Round(overallScores.Average() * 10m, 2);
            }
            else
            {
                // Final fallback to ReviewItems
                var allReviewItems = await _context.Set<ReviewItem>()
                    .Where(ri => reviewIds.Contains(ri.ReviewId))
                    .ToListAsync(cancellationToken);

                if (allReviewItems.Any())
                {
                    evaluation.OverallScore = Math.Round(allReviewItems.Average(ri => ri.RatingValue), 2);
                }
            }
        }

        // Check if score > 80 for promotion
        if (evaluation.OverallScore.HasValue && evaluation.OverallScore.Value > PROMOTION_THRESHOLD)
        {
            // Create promotion case
            var promotionCase = new PromotionCase
            {
                EvaluationId = evaluation.EvaluationId,
                RecommendedByHodId = hodUserId,
                RecommendedAt = DateTime.UtcNow,
                GmDecision = PromotionDecision.Pending,
                GmDecidedById = null,
                GmDecidedAt = null,
                DecisionReason = null
            };

            _context.Set<PromotionCase>().Add(promotionCase);

            // Create GM review
            var gmReview = new Review
            {
                EvaluationId = evaluation.EvaluationId,
                ReviewerUserId = await GetGmUserIdAsync(cancellationToken),
                ReviewerRole = ReviewerRole.GM,
                Status = REVIEW_STATUS_PENDING,
                OverallComment = null,
                SubmittedAt = null
            };

            _context.Set<Review>().Add(gmReview);

            evaluation.Status = STATUS_PENDING_GM_DECISION;

            // Notify GM
            var notification = new Notification
            {
                UserId = gmReview.ReviewerUserId,
                Subject = $"Promotion Recommendation: {evaluation.Employee?.FullName ?? "Employee"}",
                Channel = "Email",
                SentAt = DateTime.UtcNow
            };

            _context.Set<Notification>().Add(notification);
        }
        else
        {
            // No promotion, mark as completed
            evaluation.Status = STATUS_COMPLETED_NO_PROMOTION;

            // Notify employee with motivational message
            var notification = new Notification
            {
                UserId = evaluation.EmployeeId,
                Subject = "Evaluation Complete - Great Work!",
                Channel = "Email",
                SentAt = DateTime.UtcNow
            };

            _context.Set<Notification>().Add(notification);

            // TODO: Generate training recommendations and report
        }

        await Task.CompletedTask;
    }

    private async Task<int> GetHodUserIdAsync(CancellationToken cancellationToken)
    {
        var hodUser = await _context.Set<UserRole>()
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.Name == "HOD")
            .Select(ur => ur.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (hodUser == 0)
            throw new BusinessRuleException("No HOD found in the system.");

        return hodUser;
    }

    private async Task<int> GetGmUserIdAsync(CancellationToken cancellationToken)
    {
        var gmUser = await _context.Set<UserRole>()
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.Name == "GM")
            .Select(ur => ur.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (gmUser == 0)
            throw new BusinessRuleException("No GM found in the system.");

        return gmUser;
    }

    private async Task CreateNextStepNotificationAsync(Evaluation evaluation, int currentActorUserId, CancellationToken cancellationToken)
    {
        int? nextUserId = null;
        string subject = string.Empty;

        switch (evaluation.Status)
        {
            case STATUS_PENDING_TL_REVIEW:
                nextUserId = evaluation.TeamLeadId;
                subject = $"Evaluation Pending: {evaluation.Employee?.FullName ?? "Employee"}";
                break;

            case STATUS_PENDING_PEER_ASSIGNMENT:
                nextUserId = evaluation.TeamLeadId;
                subject = $"Please Assign Peer Reviewers: {evaluation.Employee?.FullName ?? "Employee"}";
                break;

            case STATUS_COMPLETED_NO_PROMOTION:
            case STATUS_COMPLETED_PROMOTION_APPROVED:
            case STATUS_COMPLETED_PROMOTION_REJECTED:
                nextUserId = evaluation.EmployeeId;
                subject = "Your Evaluation is Complete";
                break;
        }

        if (nextUserId.HasValue)
        {
            var notification = new Notification
            {
                UserId = nextUserId.Value,
                Subject = subject,
                Channel = "Email",
                SentAt = DateTime.UtcNow
            };

            _context.Set<Notification>().Add(notification);
        }

        await Task.CompletedTask;
    }

    public async Task<IEnumerable<AvailablePeerDto>> GetAvailablePeersAsync(int evaluationId, CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
            throw new NotFoundException(nameof(Evaluation), evaluationId);

        // Get all users from the database (including the employee for testing with single user)
        // In production, you may want to exclude: u.UserId != evaluation.EmployeeId
        var availablePeers = await _context.Users
            .Include(u => u.Department)
            .OrderBy(u => u.FullName)
            .Select(u => new AvailablePeerDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Department = u.Department != null ? u.Department.Name : "No Department"
            })
            .ToListAsync(cancellationToken);

        return availablePeers;
    }

    public async Task<IEnumerable<MyEvaluationDto>> GetMyEvaluationsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        var myEvaluations = new List<MyEvaluationDto>();
        var managedEmployeeIds = await _context.UserManagerMappings
            .Where(m => m.ManagerUserId == userId)
            .Select(m => m.EmployeeUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Get evaluations where I am the employee
        var myEvaluationsAsEmployee = await _context.Set<Evaluation>()
            .Include(e => e.Cycle)
            .Include(e => e.Reviews)
            .Where(e => e.EmployeeId == userId)
            .Select(e => new MyEvaluationDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee.FullName,
                Status = e.Status,
                MyRole = "Employee",
                SubmittedDate = e.Reviews
                    .Where(r => r.ReviewerRole == ReviewerRole.Self)
                    .Select(r => r.SubmittedAt)
                    .FirstOrDefault(),
                CompletedDate = e.Status.Contains("Completed") || e.Status.Contains("Rejected")
                    ? e.Reviews.OrderByDescending(r => r.SubmittedAt).Select(r => r.SubmittedAt).FirstOrDefault()
                    : null,
                CycleId = e.CycleId,
                CycleName = e.Cycle.Name,
                OverallScore = e.OverallScore
            })
            .ToListAsync(cancellationToken);

        myEvaluations.AddRange(myEvaluationsAsEmployee);

        // Get evaluations where I am the RM (legacy + workflow-v2 managed employees)
        var rmEvaluations = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Cycle)
            .Where(e =>
                (e.ReportingManagerId == userId || managedEmployeeIds.Contains(e.EmployeeId)) &&
                (
                    e.Status == STATUS_PENDING_RM_REVIEW ||
                    e.Status == "Pending_RM_Review_PostCompletion" ||
                    e.WorkflowVersion == "v2"))
            .Select(e => new MyEvaluationDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee.FullName,
                Status = e.Status,
                MyRole = "RM",
                SubmittedDate = e.Reviews
                    .Where(r => r.ReviewerRole == ReviewerRole.Self)
                    .Select(r => r.SubmittedAt)
                    .FirstOrDefault(),
                CompletedDate = e.Status.Contains("Completed") || e.Status.Contains("Rejected")
                    ? e.Reviews.OrderByDescending(r => r.SubmittedAt).Select(r => r.SubmittedAt).FirstOrDefault()
                    : null,
                CycleId = e.CycleId,
                CycleName = e.Cycle.Name,
                OverallScore = e.OverallScore
            })
            .ToListAsync(cancellationToken);

        myEvaluations.AddRange(rmEvaluations);

        // Get evaluations where I am the TL (legacy + workflow-v2 activation/review stages)
        var tlEvaluations = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Cycle)
            .Where(e =>
                e.TeamLeadId == userId &&
                (
                    e.Status == STATUS_PENDING_TL_REVIEW ||
                    e.Status == STATUS_PENDING_PEER_ASSIGNMENT ||
                    e.Status == STATUS_V2_PENDING_TL_ACTIVATION_REVIEW ||
                    e.Status == STATUS_V2_PENDING_PARALLEL_REVIEWS))
            .Select(e => new MyEvaluationDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee.FullName,
                Status = e.Status,
                MyRole = "TL",
                SubmittedDate = e.Reviews
                    .Where(r => r.ReviewerRole == ReviewerRole.Self)
                    .Select(r => r.SubmittedAt)
                    .FirstOrDefault(),
                CompletedDate = e.Status.Contains("Completed") || e.Status.Contains("Rejected")
                    ? e.Reviews.OrderByDescending(r => r.SubmittedAt).Select(r => r.SubmittedAt).FirstOrDefault()
                    : null,
                CycleId = e.CycleId,
                CycleName = e.Cycle.Name,
                OverallScore = e.OverallScore
            })
            .ToListAsync(cancellationToken);

        myEvaluations.AddRange(tlEvaluations);

        // Get evaluations where I might need to act as GM (if I have GM role)
        if (userRoles.Contains("GM"))
        {
            var gmEvaluations = await _context.Set<Evaluation>()
                .Include(e => e.Employee)
                .Include(e => e.Cycle)
                .Include(e => e.Reviews)
                .ThenInclude(r => r.ReviewScores)
                .Include(e => e.Reviews)
                .ThenInclude(r => r.ReviewItems)
                .Where(e =>
                    (
                        e.Status == STATUS_PENDING_GM_DECISION ||
                        e.Status == STATUS_V2_PENDING_GM_DECISION ||
                        e.Status == "V2_PROMOTION_DEFERRED" ||
                        e.Status.Contains("Completed")
                    ) &&
                    e.EmployeeId != userId)
                .Select(e => new MyEvaluationDto
                {
                    EvaluationId = e.EvaluationId,
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.Employee.FullName,
                    Status = e.Status,
                    MyRole = "GM",
                    SubmittedDate = e.Reviews
                        .Where(r => r.ReviewerRole == ReviewerRole.Self)
                        .Select(r => r.SubmittedAt)
                        .FirstOrDefault(),
                    CompletedDate = e.Status.Contains("Completed") || e.Status.Contains("Rejected")
                        ? e.Reviews.OrderByDescending(r => r.SubmittedAt).Select(r => r.SubmittedAt).FirstOrDefault()
                        : null,
                    CycleId = e.CycleId,
                    CycleName = e.Cycle.Name,
                    OverallScore = e.OverallScore
                })
                .ToListAsync(cancellationToken);

            myEvaluations.AddRange(gmEvaluations);
        }

        // Get evaluations where I might need to act as HR (if I have HR role)
        if (userRoles.Contains("HR"))
        {
            var hrEvaluations = await _context.Set<Evaluation>()
                .Include(e => e.Employee)
                .Include(e => e.Cycle)
                .Include(e => e.Reviews)
                .Where(e =>
                    (
                        e.Status == STATUS_PENDING_HR_PROCESSING ||
                        e.Status == STATUS_V2_PENDING_HR_PROMOTION ||
                        e.Status == STATUS_V2_PENDING_HR_LOW_PERFORMER ||
                        e.Status.Contains("Completed")
                    ) &&
                    e.EmployeeId != userId)
                .Select(e => new MyEvaluationDto
                {
                    EvaluationId = e.EvaluationId,
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.Employee.FullName,
                    Status = e.Status,
                    MyRole = "HR",
                    SubmittedDate = e.Reviews
                        .Where(r => r.ReviewerRole == ReviewerRole.Self)
                        .Select(r => r.SubmittedAt)
                        .FirstOrDefault(),
                    CompletedDate = e.Status.Contains("Completed") || e.Status.Contains("Rejected")
                        ? e.Reviews.OrderByDescending(r => r.SubmittedAt).Select(r => r.SubmittedAt).FirstOrDefault()
                        : null,
                    CycleId = e.CycleId,
                    CycleName = e.Cycle.Name,
                    OverallScore = e.OverallScore
                })
                .ToListAsync(cancellationToken);

            myEvaluations.AddRange(hrEvaluations);
        }

        // Remove duplicates and order by submitted date descending
        return myEvaluations
            .GroupBy(e => e.EvaluationId)
            .Select(g => g.First())
            .OrderByDescending(e => e.SubmittedDate ?? DateTime.MinValue);
    }

    public async Task<IEnumerable<PendingApprovalDto>> GetPendingApprovalsForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        var pendingApprovals = new List<PendingApprovalDto>();
        var managedEmployeeIds = await _context.UserManagerMappings
            .Where(m => m.ManagerUserId == userId)
            .Select(m => m.EmployeeUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Workflow-v2 employee actions (activation + self-evaluation)
        var employeeV2Actions = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Cycle)
            .Where(e =>
                e.EmployeeId == userId &&
                (e.Status == STATUS_V2_PENDING_EMPLOYEE_ACTIVATION ||
                 e.Status == STATUS_V2_RETURNED_FOR_ACTIVATION ||
                 e.Status == STATUS_V2_ACTIVE_GOALS))
            .Select(e => new PendingApprovalDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee.FullName,
                Status = e.Status,
                RequiredRole = "Employee",
                SubmittedDate = e.Reviews
                    .Where(r => r.ReviewerRole == ReviewerRole.Self)
                    .Select(r => r.SubmittedAt)
                    .FirstOrDefault(),
                CycleId = e.CycleId,
                CycleName = e.Cycle.Name
            })
            .ToListAsync(cancellationToken);

        pendingApprovals.AddRange(employeeV2Actions);

        // Legacy RM first approval
        var rmApprovalsFirst = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Cycle)
            .Where(e =>
                (e.ReportingManagerId == userId || managedEmployeeIds.Contains(e.EmployeeId)) &&
                e.Status == STATUS_PENDING_RM_REVIEW)
            .Select(e => new PendingApprovalDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee.FullName,
                Status = e.Status,
                RequiredRole = "RM",
                SubmittedDate = e.Reviews
                    .Where(r => r.ReviewerRole == ReviewerRole.Self)
                    .Select(r => r.SubmittedAt)
                    .FirstOrDefault(),
                CycleId = e.CycleId,
                CycleName = e.Cycle.Name
            })
            .ToListAsync(cancellationToken);

        pendingApprovals.AddRange(rmApprovalsFirst);

        // Legacy RM second approval
        var rmApprovalsSecond = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Cycle)
            .Where(e =>
                (e.ReportingManagerId == userId || managedEmployeeIds.Contains(e.EmployeeId)) &&
                e.Status == "Pending_RM_Review_PostCompletion")
            .Select(e => new PendingApprovalDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee.FullName,
                Status = e.Status,
                RequiredRole = "RM",
                SubmittedDate = e.Reviews
                    .Where(r => r.ReviewerRole == ReviewerRole.Self)
                    .Select(r => r.SubmittedAt)
                    .FirstOrDefault(),
                CycleId = e.CycleId,
                CycleName = e.Cycle.Name
            })
            .ToListAsync(cancellationToken);

        pendingApprovals.AddRange(rmApprovalsSecond);

        // Workflow-v2 RM review task (parallel stage)
        var rmV2ParallelApprovals = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Cycle)
            .Include(e => e.Reviews)
            .Where(e =>
                e.Status == STATUS_V2_PENDING_PARALLEL_REVIEWS &&
                e.Reviews.Any(r =>
                    r.ReviewerRole == ReviewerRole.RM &&
                    r.ReviewerUserId == userId &&
                    r.Status == REVIEW_STATUS_PENDING))
            .Select(e => new PendingApprovalDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee.FullName,
                Status = e.Status,
                RequiredRole = "RM",
                SubmittedDate = e.Reviews
                    .Where(r => r.ReviewerRole == ReviewerRole.Self)
                    .Select(r => r.SubmittedAt)
                    .FirstOrDefault(),
                CycleId = e.CycleId,
                CycleName = e.Cycle.Name
            })
            .ToListAsync(cancellationToken);

        pendingApprovals.AddRange(rmV2ParallelApprovals);

        // Legacy TL approvals
        var tlApprovals = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Cycle)
            .Where(e => e.TeamLeadId == userId && (e.Status == STATUS_PENDING_TL_REVIEW || e.Status == STATUS_PENDING_PEER_ASSIGNMENT))
            .Select(e => new PendingApprovalDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee.FullName,
                Status = e.Status,
                RequiredRole = "TL",
                SubmittedDate = e.Reviews
                    .Where(r => r.ReviewerRole == ReviewerRole.RM)
                    .Select(r => r.SubmittedAt)
                    .FirstOrDefault(),
                CycleId = e.CycleId,
                CycleName = e.Cycle.Name
            })
            .ToListAsync(cancellationToken);

        pendingApprovals.AddRange(tlApprovals);

        // Workflow-v2 TL activation review
        var tlActivationApprovals = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Cycle)
            .Where(e => e.TeamLeadId == userId && e.Status == STATUS_V2_PENDING_TL_ACTIVATION_REVIEW)
            .Select(e => new PendingApprovalDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee.FullName,
                Status = e.Status,
                RequiredRole = "TL",
                SubmittedDate = null,
                CycleId = e.CycleId,
                CycleName = e.Cycle.Name
            })
            .ToListAsync(cancellationToken);

        pendingApprovals.AddRange(tlActivationApprovals);

        // Workflow-v2 TL parallel review
        var tlV2ParallelApprovals = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Cycle)
            .Include(e => e.Reviews)
            .Where(e =>
                e.Status == STATUS_V2_PENDING_PARALLEL_REVIEWS &&
                e.Reviews.Any(r =>
                    r.ReviewerRole == ReviewerRole.TL &&
                    r.ReviewerUserId == userId &&
                    r.Status == REVIEW_STATUS_PENDING))
            .Select(e => new PendingApprovalDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee.FullName,
                Status = e.Status,
                RequiredRole = "TL",
                SubmittedDate = e.Reviews
                    .Where(r => r.ReviewerRole == ReviewerRole.Self)
                    .Select(r => r.SubmittedAt)
                    .FirstOrDefault(),
                CycleId = e.CycleId,
                CycleName = e.Cycle.Name
            })
            .ToListAsync(cancellationToken);

        pendingApprovals.AddRange(tlV2ParallelApprovals);

        // Legacy peer approvals
        var peerApprovals = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Cycle)
            .Include(e => e.PeerAssignments)
            .Include(e => e.Reviews)
            .Where(e => e.Status == STATUS_PENDING_PEER_REVIEWS && 
                   e.PeerAssignments.Any(pa => pa.PeerUserId == userId) &&
                   e.Reviews.Any(r => r.ReviewerUserId == userId && r.ReviewerRole == ReviewerRole.Peer && r.Status == REVIEW_STATUS_PENDING))
            .Select(e => new PendingApprovalDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee.FullName,
                Status = e.Status,
                RequiredRole = "Peer",
                SubmittedDate = e.Reviews
                    .Where(r => r.ReviewerRole == ReviewerRole.TL)
                    .Select(r => r.SubmittedAt)
                    .FirstOrDefault(),
                CycleId = e.CycleId,
                CycleName = e.Cycle.Name
            })
            .ToListAsync(cancellationToken);

        pendingApprovals.AddRange(peerApprovals);

        // Workflow-v2 peer approvals
        var peerV2Approvals = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Cycle)
            .Include(e => e.Reviews)
            .Where(e =>
                e.Status == STATUS_V2_PENDING_PARALLEL_REVIEWS &&
                e.Reviews.Any(r =>
                    r.ReviewerRole == ReviewerRole.Peer &&
                    r.ReviewerUserId == userId &&
                    r.Status == REVIEW_STATUS_PENDING))
            .Select(e => new PendingApprovalDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee.FullName,
                Status = e.Status,
                RequiredRole = "Peer",
                SubmittedDate = e.Reviews
                    .Where(r => r.ReviewerRole == ReviewerRole.Self)
                    .Select(r => r.SubmittedAt)
                    .FirstOrDefault(),
                CycleId = e.CycleId,
                CycleName = e.Cycle.Name
            })
            .ToListAsync(cancellationToken);

        pendingApprovals.AddRange(peerV2Approvals);

        // HOD approvals (legacy + workflow-v2 with department mapping)
        if (userRoles.Contains("HOD"))
        {
            var hodDeptIds = await _context.DepartmentHodMappings
                .Where(m => m.HodUserId == userId)
                .Select(m => m.DeptId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var hodApprovalsQuery = _context.Set<Evaluation>()
                .Include(e => e.Employee)
                .Include(e => e.Cycle)
                .Where(e => e.Status == STATUS_PENDING_HOD_REVIEW || e.Status == STATUS_V2_PENDING_HOD_REVIEW);

            if (hodDeptIds.Count > 0)
            {
                hodApprovalsQuery = hodApprovalsQuery.Where(e => hodDeptIds.Contains(e.Employee.DeptId));
            }

            var hodApprovals = await hodApprovalsQuery
                .Select(e => new PendingApprovalDto
                {
                    EvaluationId = e.EvaluationId,
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.Employee.FullName,
                    Status = e.Status,
                    RequiredRole = "HOD",
                    SubmittedDate = e.Reviews
                        .Where(r =>
                            r.ReviewerRole == ReviewerRole.Peer ||
                            r.ReviewerRole == ReviewerRole.RM ||
                            r.ReviewerRole == ReviewerRole.TL)
                        .OrderByDescending(r => r.SubmittedAt)
                        .Select(r => r.SubmittedAt)
                        .FirstOrDefault(),
                    CycleId = e.CycleId,
                    CycleName = e.Cycle.Name
                })
                .ToListAsync(cancellationToken);

            pendingApprovals.AddRange(hodApprovals);
        }

        // GM approvals (legacy + workflow-v2)
        if (userRoles.Contains("GM"))
        {
            var gmApprovals = await _context.Set<Evaluation>()
                .Include(e => e.Employee)
                .Include(e => e.Cycle)
                .Where(e => e.Status == STATUS_PENDING_GM_DECISION || e.Status == STATUS_V2_PENDING_GM_DECISION)
                .Select(e => new PendingApprovalDto
                {
                    EvaluationId = e.EvaluationId,
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.Employee.FullName,
                    Status = e.Status,
                    RequiredRole = "GM",
                    SubmittedDate = e.Reviews
                        .Where(r => r.ReviewerRole == ReviewerRole.HOD)
                        .Select(r => r.SubmittedAt)
                        .FirstOrDefault(),
                    CycleId = e.CycleId,
                    CycleName = e.Cycle.Name
                })
                .ToListAsync(cancellationToken);

            pendingApprovals.AddRange(gmApprovals);
        }

        // HR approvals (legacy + workflow-v2 promotion + low performer queue)
        if (userRoles.Contains("HR"))
        {
            var hrApprovals = await _context.Set<Evaluation>()
                .Include(e => e.Employee)
                .Include(e => e.Cycle)
                .Include(e => e.Reviews)
                .Where(e =>
                    e.Status == STATUS_PENDING_HR_PROCESSING ||
                    e.Status == STATUS_V2_PENDING_HR_PROMOTION ||
                    e.Status == STATUS_V2_PENDING_HR_LOW_PERFORMER)
                .Select(e => new PendingApprovalDto
                {
                    EvaluationId = e.EvaluationId,
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.Employee.FullName,
                    Status = e.Status,
                    RequiredRole = "HR",
                    SubmittedDate = e.Reviews
                        .Where(r => r.ReviewerRole == ReviewerRole.Self)
                        .Select(r => r.SubmittedAt)
                        .FirstOrDefault(),
                    CycleId = e.CycleId,
                    CycleName = e.Cycle.Name
                })
                .ToListAsync(cancellationToken);

            pendingApprovals.AddRange(hrApprovals);
        }

        return pendingApprovals.OrderByDescending(p => p.SubmittedDate);
    }

    public async Task<BulkApprovalStatsDto> GetBulkApprovalStatsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        
        if (!userRoles.Contains("GM") && !userRoles.Contains("HR"))
            throw new BusinessRuleException("Only GM or HR can view bulk approval statistics.");

        var pendingGmCount = await _context.Set<Evaluation>()
            .Where(e => e.Status == STATUS_PENDING_GM_DECISION || e.Status == STATUS_V2_PENDING_GM_DECISION)
            .CountAsync(cancellationToken);

        var pendingHrCount = await _context.Set<Evaluation>()
            .Where(e => e.Status == STATUS_PENDING_HR_PROCESSING || e.Status == STATUS_V2_PENDING_HR_PROMOTION)
            .CountAsync(cancellationToken);

        // Get evaluations with their reviews to calculate scores
        var evaluations = await _context.Set<Evaluation>()
            .Include(e => e.Reviews)
                .ThenInclude(r => r.ReviewScores)
            .Include(e => e.Reviews)
                .ThenInclude(r => r.ReviewItems)
            .Where(e =>
                e.Status == STATUS_PENDING_GM_DECISION ||
                e.Status == STATUS_PENDING_HR_PROCESSING ||
                e.Status == STATUS_V2_PENDING_GM_DECISION ||
                e.Status == STATUS_V2_PENDING_HR_PROMOTION)
            .ToListAsync(cancellationToken);

        // Calculate scores for each evaluation
        var calculatedScores = evaluations.Select(e =>
        {
            if (e.OverallScore.HasValue && e.OverallScore.Value > 0)
                return e.OverallScore.Value;
            
            var allReviewScores = e.Reviews
                .Where(r => r.ReviewScores != null)
                .SelectMany(r => r.ReviewScores)
                .ToList();
            
            if (allReviewScores.Any())
                return Math.Round(allReviewScores.Average(rs => rs.ScoreValue) * 10m, 2);
            
            var allReviewItems = e.Reviews
                .Where(r => r.ReviewItems != null)
                .SelectMany(r => r.ReviewItems)
                .ToList();
            
            if (allReviewItems.Any())
                return Math.Round(allReviewItems.Average(ri => ri.RatingValue), 2);
            
            var hodReview = e.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.HOD && r.OverallScore.HasValue);
            if (hodReview?.OverallScore.HasValue == true)
                return hodReview.OverallScore.Value * 10m;
            
            return 0m;
        }).ToList();

        var eligibleCount = calculatedScores.Count(s => s >= PROMOTION_THRESHOLD);
        var notEligibleCount = calculatedScores.Count(s => s < PROMOTION_THRESHOLD);
        var avgScore = calculatedScores.Any() ? calculatedScores.Average() : 0m;

        return new BulkApprovalStatsDto
        {
            PendingGmApproval = pendingGmCount,
            PendingHrProcessing = pendingHrCount,
            EligibleForPromotion = eligibleCount,
            NotEligibleForPromotion = notEligibleCount,
            AverageScore = Math.Round(avgScore, 2)
        };
    }

    public async Task<IEnumerable<BulkApprovalCandidateDto>> GetPendingGmBulkApprovalsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        
        if (!userRoles.Contains("GM"))
            throw new BusinessRuleException("Only GM can view pending bulk approvals.");

        var evaluations = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Cycle)
            .Include(e => e.PromotionCases)
                .ThenInclude(pc => pc.RecommendedByHod)
            .Include(e => e.Reviews)
                .ThenInclude(r => r.ReviewScores)
            .Include(e => e.Reviews)
                .ThenInclude(r => r.ReviewItems)
            .Where(e => e.Status == STATUS_PENDING_GM_DECISION || e.Status == STATUS_V2_PENDING_GM_DECISION)
            .OrderByDescending(e => e.OverallScore)
            .ToListAsync(cancellationToken);

        return evaluations.Select(e => 
        {
            var promotionCase = e.PromotionCases.FirstOrDefault();
            
            // Calculate score - use OverallScore if available, otherwise calculate from reviews
            decimal scorePercentage = 0m;
            
            if (e.OverallScore.HasValue && e.OverallScore.Value > 0)
            {
                scorePercentage = e.OverallScore.Value;
            }
            else
            {
                // Try to calculate from ReviewScores
                var allReviewScores = e.Reviews
                    .Where(r => r.ReviewScores != null)
                    .SelectMany(r => r.ReviewScores)
                    .ToList();
                
                if (allReviewScores.Any())
                {
                    scorePercentage = Math.Round(allReviewScores.Average(rs => rs.ScoreValue) * 10m, 2);
                }
                else
                {
                    var allReviewItems = e.Reviews
                        .Where(r => r.ReviewItems != null)
                        .SelectMany(r => r.ReviewItems)
                        .ToList();
                    
                    if (allReviewItems.Any())
                    {
                        scorePercentage = Math.Round(allReviewItems.Average(ri => ri.RatingValue), 2);
                    }
                    else
                    {
                        var hodReview = e.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.HOD && r.OverallScore.HasValue);
                        if (hodReview?.OverallScore.HasValue == true)
                            scorePercentage = hodReview.OverallScore.Value * 10m;
                    }
                }
            }
            
            return new BulkApprovalCandidateDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee?.FullName ?? "Unknown",
                EmployeeEmail = e.Employee?.Email ?? "",
                Status = e.Status,
                OverallScore = e.OverallScore ?? scorePercentage,
                ScorePercentage = scorePercentage,
                IsEligibleForPromotion = scorePercentage >= PROMOTION_THRESHOLD,
                CycleId = e.CycleId,
                CycleName = e.Cycle?.Name ?? "",
                LastReviewedAt = promotionCase?.GmDecidedAt,
                RecommendedByHodName = promotionCase?.RecommendedByHod?.FullName,
                RecommendedAt = promotionCase?.RecommendedAt
            };
        });
    }

    public async Task<IEnumerable<BulkApprovalCandidateDto>> GetPendingHrBulkProcessingAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        
        if (!userRoles.Contains("HR"))
            throw new BusinessRuleException("Only HR can view pending bulk processing.");

        var evaluations = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Cycle)
            .Include(e => e.PromotionCases)
                .ThenInclude(pc => pc.RecommendedByHod)
            .Include(e => e.PromotionCases)
                .ThenInclude(pc => pc.GmDecidedBy)
            .Include(e => e.Reviews)
                .ThenInclude(r => r.ReviewScores)
            .Include(e => e.Reviews)
                .ThenInclude(r => r.ReviewItems)
            .Where(e => e.Status == STATUS_PENDING_HR_PROCESSING || e.Status == STATUS_V2_PENDING_HR_PROMOTION)
            .OrderByDescending(e => e.OverallScore)
            .ToListAsync(cancellationToken);

        return evaluations.Select(e => 
        {
            var promotionCase = e.PromotionCases.FirstOrDefault();
            
            // Calculate score - use OverallScore if available, otherwise calculate from reviews
            decimal scorePercentage = 0m;
            
            if (e.OverallScore.HasValue && e.OverallScore.Value > 0)
            {
                scorePercentage = e.OverallScore.Value;
            }
            else
            {
                // Try to calculate from ReviewScores
                var allReviewScores = e.Reviews
                    .Where(r => r.ReviewScores != null)
                    .SelectMany(r => r.ReviewScores)
                    .ToList();
                
                if (allReviewScores.Any())
                {
                    scorePercentage = Math.Round(allReviewScores.Average(rs => rs.ScoreValue) * 10m, 2);
                }
                else
                {
                    var allReviewItems = e.Reviews
                        .Where(r => r.ReviewItems != null)
                        .SelectMany(r => r.ReviewItems)
                        .ToList();
                    
                    if (allReviewItems.Any())
                    {
                        scorePercentage = Math.Round(allReviewItems.Average(ri => ri.RatingValue), 2);
                    }
                    else
                    {
                        var hodReview = e.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.HOD && r.OverallScore.HasValue);
                        if (hodReview?.OverallScore.HasValue == true)
                            scorePercentage = hodReview.OverallScore.Value * 10m;
                    }
                }
            }
            
            return new BulkApprovalCandidateDto
            {
                EvaluationId = e.EvaluationId,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee?.FullName ?? "Unknown",
                EmployeeEmail = e.Employee?.Email ?? "",
                Status = e.Status,
                OverallScore = e.OverallScore ?? scorePercentage,
                ScorePercentage = scorePercentage,
                IsEligibleForPromotion = scorePercentage >= PROMOTION_THRESHOLD,
                CycleId = e.CycleId,
                CycleName = e.Cycle?.Name ?? "",
                LastReviewedAt = promotionCase?.GmDecidedAt,
                RecommendedByHodName = promotionCase?.RecommendedByHod?.FullName,
                RecommendedAt = promotionCase?.RecommendedAt
            };
        });
    }

    public async Task<BulkApprovalResponseDto> GmBulkApproveAsync(int gmUserId, BulkApprovalRequestDto request, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(gmUserId, cancellationToken);
        
        if (!userRoles.Contains("GM"))
            throw new BusinessRuleException("Only GM can perform bulk approvals.");

        if (request.EvaluationIds == null || !request.EvaluationIds.Any())
            throw new BusinessRuleException("At least one evaluation must be selected for bulk approval.");

        var results = new List<BulkApprovalResultItemDto>();
        var successCount = 0;
        var failedCount = 0;

        foreach (var evaluationId in request.EvaluationIds)
        {
            try
            {
                var evaluation = await _context.Set<Evaluation>()
                    .Include(e => e.Employee)
                    .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

                if (evaluation == null)
                {
                    throw new NotFoundException(nameof(Evaluation), evaluationId);
                }

                if (evaluation.WorkflowVersion == "v2" || evaluation.Status == STATUS_V2_PENDING_GM_DECISION)
                {
                    await _workflowV2Service.GmDecisionAsync(
                        evaluationId,
                        gmUserId,
                        new GmV2DecisionDto
                        {
                            Approve = true,
                            VacancyAvailable = true,
                            Comment = request.Comment ?? "Bulk approved by GM"
                        },
                        cancellationToken);
                }
                else
                {
                    await ProcessPromotionDecisionAsync(
                        evaluationId,
                        gmUserId,
                        true,
                        request.Comment ?? "Bulk approved by GM",
                        cancellationToken);
                }

                results.Add(new BulkApprovalResultItemDto
                {
                    EvaluationId = evaluationId,
                    EmployeeName = evaluation?.Employee?.FullName ?? "Unknown",
                    Success = true,
                    Message = "Approved successfully",
                    NewStatus = evaluation.WorkflowVersion == "v2" ? STATUS_V2_PENDING_HR_PROMOTION : STATUS_PENDING_HR_PROCESSING
                });
                successCount++;
            }
            catch (Exception ex)
            {
                var evaluation = await _context.Set<Evaluation>()
                    .Include(e => e.Employee)
                    .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

                results.Add(new BulkApprovalResultItemDto
                {
                    EvaluationId = evaluationId,
                    EmployeeName = evaluation?.Employee?.FullName ?? "Unknown",
                    Success = false,
                    Message = ex.Message
                });
                failedCount++;
            }
        }

        return new BulkApprovalResponseDto
        {
            TotalRequested = request.EvaluationIds.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            Message = $"Bulk approval completed: {successCount} approved, {failedCount} failed",
            Results = results
        };
    }

    public async Task<BulkApprovalResponseDto> HrBulkProcessAsync(int hrUserId, BulkApprovalRequestDto request, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(hrUserId, cancellationToken);
        
        if (!userRoles.Contains("HR"))
            throw new BusinessRuleException("Only HR can perform bulk processing.");

        if (request.EvaluationIds == null || !request.EvaluationIds.Any())
            throw new BusinessRuleException("At least one evaluation must be selected for bulk processing.");

        var results = new List<BulkApprovalResultItemDto>();
        var successCount = 0;
        var failedCount = 0;

        foreach (var evaluationId in request.EvaluationIds)
        {
            try
            {
                var evaluation = await _context.Set<Evaluation>()
                    .Include(e => e.Employee)
                    .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

                if (evaluation == null)
                {
                    throw new NotFoundException(nameof(Evaluation), evaluationId);
                }

                await FinalizePromotionByHrAsync(
                    evaluationId,
                    hrUserId,
                    true,
                    request.Comment ?? "Bulk processed by HR",
                    cancellationToken);

                results.Add(new BulkApprovalResultItemDto
                {
                    EvaluationId = evaluationId,
                    EmployeeName = evaluation?.Employee?.FullName ?? "Unknown",
                    Success = true,
                    Message = "Promotion processed successfully",
                    NewStatus = evaluation.WorkflowVersion == "v2"
                        ? STATUS_V2_COMPLETED_PROMOTION_APPROVED
                        : STATUS_COMPLETED_PROMOTION_APPROVED
                });
                successCount++;
            }
            catch (Exception ex)
            {
                var evaluation = await _context.Set<Evaluation>()
                    .Include(e => e.Employee)
                    .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

                results.Add(new BulkApprovalResultItemDto
                {
                    EvaluationId = evaluationId,
                    EmployeeName = evaluation?.Employee?.FullName ?? "Unknown",
                    Success = false,
                    Message = ex.Message
                });
                failedCount++;
            }
        }

        return new BulkApprovalResponseDto
        {
            TotalRequested = request.EvaluationIds.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            Message = $"Bulk processing completed: {successCount} processed, {failedCount} failed",
            Results = results
        };
    }

    public async Task HodSubmitScoreAsync(int evaluationId, int hodUserId, decimal score, string? comment, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(hodUserId, cancellationToken);
        
        if (!userRoles.Contains("HOD"))
            throw new BusinessRuleException("Only HOD can submit scores at this stage.");

        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.Reviews)
            .Include(e => e.Employee)
            .Include(e => e.PromotionCases)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
            throw new NotFoundException(nameof(Evaluation), evaluationId);

        if (evaluation.Status != STATUS_PENDING_HOD_REVIEW)
            throw new BusinessRuleException($"Evaluation must be at HOD review stage. Current status: {evaluation.Status}");

        if (score < 1 || score > 10)
            throw new BusinessRuleException("Score must be between 1 and 10.");

        var oldStatus = evaluation.Status;
        var scorePercentage = score * 10m;
        evaluation.OverallScore = scorePercentage;

        var hodReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.HOD);
        if (hodReview != null)
        {
            hodReview.Status = REVIEW_STATUS_APPROVED;
            hodReview.OverallComment = comment;
            hodReview.OverallScore = score;
            hodReview.SubmittedAt = DateTime.UtcNow;
        }

        if (scorePercentage >= PROMOTION_THRESHOLD)
        {
            var promotionCase = evaluation.PromotionCases.FirstOrDefault();
            if (promotionCase == null)
            {
                promotionCase = new PromotionCase
                {
                    EvaluationId = evaluation.EvaluationId,
                    RecommendedByHodId = hodUserId,
                    RecommendedAt = DateTime.UtcNow,
                    GmDecision = PromotionDecision.Pending
                };
                _context.Set<PromotionCase>().Add(promotionCase);
            }
            else
            {
                promotionCase.RecommendedByHodId = hodUserId;
                promotionCase.RecommendedAt = DateTime.UtcNow;
                promotionCase.GmDecision = PromotionDecision.Pending;
            }

            evaluation.Status = STATUS_PENDING_GM_DECISION;

            // Notify GM for second approval
            var gmNotification = new Notification
            {
                UserId = evaluation.ReportingManagerId,
                Subject = $"Goals Completed - Final Review Required: {evaluation.Employee?.FullName ?? "Employee"}",
                Channel = "Email",
                SentAt = DateTime.UtcNow
            };

            _context.Set<Notification>().Add(gmNotification);
        }
        else
        {
            evaluation.Status = STATUS_COMPLETED_NO_PROMOTION;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignPeerReviewersAsync(int evaluationId, int teamLeadUserId, int peerUserId1, int peerUserId2, CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.PeerAssignments)
            .Include(e => e.Reviews)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
            throw new NotFoundException(nameof(Evaluation), evaluationId);

        if (evaluation.TeamLeadId != teamLeadUserId)
            throw new BusinessRuleException("Only the assigned Team Lead can assign peer reviewers.");

        if (evaluation.Status != STATUS_PENDING_PEER_ASSIGNMENT)
            throw new BusinessRuleException("Peer reviewers can only be assigned after Team Lead approval.");

        // Verify peer users exist
        var peer1 = await _context.Users.FindAsync(new object[] { peerUserId1 }, cancellationToken);
        var peer2 = await _context.Users.FindAsync(new object[] { peerUserId2 }, cancellationToken);

        if (peer1 == null || peer2 == null)
            throw new NotFoundException("One or both peer reviewers not found.");

        var oldStatus = evaluation.Status;

        // Create peer assignments
        var peerAssignment1 = new PeerAssignment { EvaluationId = evaluationId, PeerUserId = peerUserId1 };
        var peerAssignment2 = new PeerAssignment { EvaluationId = evaluationId, PeerUserId = peerUserId2 };

        _context.Set<PeerAssignment>().Add(peerAssignment1);
        _context.Set<PeerAssignment>().Add(peerAssignment2);

        // Create peer review records
        var peerReview1 = new Review { EvaluationId = evaluationId, ReviewerUserId = peerUserId1, ReviewerRole = ReviewerRole.Peer, Status = REVIEW_STATUS_PENDING };
        var peerReview2 = new Review { EvaluationId = evaluationId, ReviewerUserId = peerUserId2, ReviewerRole = ReviewerRole.Peer, Status = REVIEW_STATUS_PENDING };

        _context.Set<Review>().Add(peerReview1);
        _context.Set<Review>().Add(peerReview2);

        evaluation.Status = STATUS_PENDING_PEER_REVIEWS;

        // Create approval history
        var approvalHistory = new ApprovalHistory
        {
            EvaluationId = evaluationId, ActorUserId = teamLeadUserId, ActorRole = "TL",
            Action = "AssignedPeers", Comment = $"Assigned peer reviewers: {peer1.FullName}, {peer2.FullName}",
            FromStatus = oldStatus, ToStatus = evaluation.Status, CreatedAt = DateTime.UtcNow
        };
        _context.Set<ApprovalHistory>().Add(approvalHistory);

        // Notify peers
        _context.Set<Notification>().Add(new Notification { UserId = peerUserId1, Subject = $"Peer Review Request: {evaluation.Employee?.FullName ?? "Employee"}", Channel = "Email", SentAt = DateTime.UtcNow });
        _context.Set<Notification>().Add(new Notification { UserId = peerUserId2, Subject = $"Peer Review Request: {evaluation.Employee?.FullName ?? "Employee"}", Channel = "Email", SentAt = DateTime.UtcNow });

        await _context.SaveChangesAsync(cancellationToken);

        // Send emails to peer reviewers
        var employee = evaluation.Employee ?? await _context.Users.FindAsync(new object[] { evaluation.EmployeeId }, cancellationToken);
        if (employee != null)
        {
            await _emailService.SendEvaluationNotificationAsync(peer1.Email, peer1.FullName, employee.FullName, "Assigned", "Peer Reviewer", "You have been assigned as a peer reviewer.", evaluationId, cancellationToken);
            await _emailService.SendEvaluationNotificationAsync(peer2.Email, peer2.FullName, employee.FullName, "Assigned", "Peer Reviewer", "You have been assigned as a peer reviewer.", evaluationId, cancellationToken);
        }
    }

    public async Task<EvaluationDetailDto> GetEvaluationDetailsAsync(int evaluationId, int userId, CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.Employee).Include(e => e.ReportingManager).Include(e => e.TeamLead).Include(e => e.Cycle)
            .Include(e => e.Reviews).ThenInclude(r => r.Reviewer)
            .Include(e => e.Reviews).ThenInclude(r => r.ReviewScores).ThenInclude(rs => rs.PersonalGoal)
            .Include(e => e.Reviews).ThenInclude(r => r.ReviewItems)
            .Include(e => e.EmployeeGoals)
            .Include(e => e.PeerAssignments).ThenInclude(pa => pa.PeerUser)
            .Include(e => e.PromotionCases).ThenInclude(pc => pc.RecommendedByHod)
            .Include(e => e.PromotionCases).ThenInclude(pc => pc.GmDecidedBy)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
            throw new NotFoundException(nameof(Evaluation), evaluationId);

        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        var isMappedManager = await _context.UserManagerMappings
            .AnyAsync(
                m => m.ManagerUserId == userId && m.EmployeeUserId == evaluation.EmployeeId,
                cancellationToken);

        var isMappedHod = await _context.DepartmentHodMappings
            .AnyAsync(
                m => m.HodUserId == userId && m.DeptId == evaluation.Employee.DeptId,
                cancellationToken);

        var isAuthorized = userId == evaluation.EmployeeId ||
                           userId == evaluation.ReportingManagerId ||
                           userId == evaluation.TeamLeadId ||
                           isMappedManager ||
                           isMappedHod ||
                           evaluation.PeerAssignments.Any(pa => pa.PeerUserId == userId) ||
                           userRoles.Contains("HOD") ||
                           userRoles.Contains("GM") ||
                           userRoles.Contains("HR") ||
                           userRoles.Contains("Admin");

        if (!isAuthorized)
            throw new BusinessRuleException("You do not have permission to view this evaluation.");

        var approvalHistory = await _context.Set<ApprovalHistory>()
            .Include(ah => ah.ActorUser).Where(ah => ah.EvaluationId == evaluationId).OrderBy(ah => ah.CreatedAt)
            .Select(ah => new ApprovalHistoryItemDto { Id = ah.Id, ActorUserId = ah.ActorUserId, ActorName = ah.ActorUser != null ? ah.ActorUser.FullName : "Unknown", ActorRole = ah.ActorRole, Action = ah.Action, Comment = ah.Comment, FromStatus = ah.FromStatus, ToStatus = ah.ToStatus, CreatedAt = ah.CreatedAt })
            .ToListAsync(cancellationToken);

        // Get personal goal IDs from employee goals
        var personalGoalIds = evaluation.EmployeeGoals
            .Where(eg => eg.PersonalGoalId.HasValue)
            .Select(eg => eg.PersonalGoalId!.Value)
            .ToList();

        // Load personal goals with activities and framework metadata
        // Navigation: PersonalGoal -> GoalItem (ScoreItem) -> Category (ScoreCategory)
        var personalGoals = await _context.PersonalGoals
            .Include(pg => pg.Activities)
            .Include(pg => pg.GoalItem)
                .ThenInclude(gi => gi.Category)
            .Where(pg => personalGoalIds.Contains(pg.Id))
            .ToListAsync(cancellationToken);

        var goalAssignments = await _context.GoalAssignments
            .Where(ga => ga.GoalSetId == evaluation.GoalSetId && ga.AssignedToUserId == evaluation.EmployeeId)
            .ToListAsync(cancellationToken);

        // Load all per-goal ReviewScores for this evaluation (with reviewer info)
        var allGoalReviewScores = await _context.Set<ReviewScore>()
            .Include(rs => rs.Review)
            .Include(rs => rs.Reviewer)
            .Where(rs => rs.EvaluationId == evaluationId && rs.PersonalGoalId != null)
            .ToListAsync(cancellationToken);

        // Build goals with full details including activities and per-goal reviewer scores
        var goalsWithDetails = evaluation.EmployeeGoals.Select(g => {
            var personalGoal = g.PersonalGoalId.HasValue 
                ? personalGoals.FirstOrDefault(pg => pg.Id == g.PersonalGoalId.Value) 
                : null;

            var goalAssignment = g.PersonalGoalId.HasValue
                ? goalAssignments.FirstOrDefault(ga => ga.PersonalGoalId == g.PersonalGoalId.Value)
                : null;

            // Get all reviewer scores for this specific goal
            var goalScores = g.PersonalGoalId.HasValue
                ? allGoalReviewScores
                    .Where(rs => rs.PersonalGoalId == g.PersonalGoalId.Value)
                    .Select(rs => new GoalReviewerScoreDto
                    {
                        ReviewerId = rs.ReviewerId,
                        ReviewerName = rs.Reviewer?.FullName ?? "Unknown",
                        ReviewerRole = rs.Review?.ReviewerRole.ToString() ?? "Unknown",
                        ScoreValue = rs.ScoreValue,
                        Comment = rs.Comment,
                        ScoredAt = rs.CreatedAt
                    })
                    .ToList()
                : new List<GoalReviewerScoreDto>();

            decimal? averageReviewScore = goalScores.Count > 0
                ? Math.Round(goalScores.Average(s => s.ScoreValue), 2)
                : null;

            return new GoalDto 
            { 
                GoalId = g.GoalId, 
                Title = g.Title, 
                Description = g.Description, 
                WeightPct = g.WeightPct, 
                EvidenceUri = g.EvidenceUri, 
                PersonalGoalId = g.PersonalGoalId,
                GoalAssignmentId = goalAssignment?.Id,
                ActivationMethod = goalAssignment?.ActivationMethod,
                ActivationSubmittedAt = goalAssignment?.ActivationSubmittedAt,
                ActivationStatus = goalAssignment?.ActivationStatus,
                ActivationTlComment = goalAssignment?.ActivationTlComment,
                ActivationReviewedAt = goalAssignment?.ActivationReviewedAt,
                // Additional details from PersonalGoal
                TargetScore = personalGoal?.TargetScore ?? 0,
                CurrentScore = personalGoal?.CurrentScore ?? 0,
                ProgressPercent = personalGoal != null && personalGoal.TargetScore > 0 
                    ? Math.Round((personalGoal.CurrentScore / personalGoal.TargetScore) * 100, 2) 
                    : 0,
                Status = personalGoal?.Status.ToString() ?? "Unknown",
                StartDate = personalGoal?.StartDate,
                DueDate = personalGoal?.DueDate,
                StartedAt = personalGoal?.StartedAt,
                CompletedAt = personalGoal?.CompletedAt,
                // Framework metadata: GoalItem is a ScoreItem, Category is ScoreCategory
                CategoryName = personalGoal?.GoalItem?.Category?.Name,
                ItemName = personalGoal?.GoalItem?.Name,
                GoalItemName = personalGoal?.GoalItem?.Name,
                // Activities
                Activities = personalGoal?.Activities?.Select(a => new GoalActivityDto
                {
                    Id = a.Id,
                    Description = a.Description,
                    IsFromTemplate = a.IsFromTemplate,
                    Status = a.Status,
                    DueDate = a.DueDate,
                    EvidenceUrl = a.EvidenceUrl,
                    EvidenceNotes = a.EvidenceNotes
                }).ToList() ?? new List<GoalActivityDto>(),
                // Per-goal reviewer scores
                ReviewerScores = goalScores,
                AverageReviewScore = averageReviewScore
            };
        }).ToList();

        return new EvaluationDetailDto
        {
            EvaluationId = evaluation.EvaluationId, CycleId = evaluation.CycleId, CycleName = evaluation.Cycle?.Name ?? "", EmployeeId = evaluation.EmployeeId, GoalSetId = evaluation.GoalSetId,
            EmployeeName = evaluation.Employee?.FullName ?? "", EmployeeEmail = evaluation.Employee?.Email ?? "",
            ReportingManagerId = evaluation.ReportingManagerId, ReportingManagerName = evaluation.ReportingManager?.FullName ?? "",
            TeamLeadId = evaluation.TeamLeadId, TeamLeadName = evaluation.TeamLead?.FullName ?? "",
            Status = evaluation.Status, WorkflowVersion = evaluation.WorkflowVersion, OverallScore = evaluation.OverallScore,
            Reviews = evaluation.Reviews.Select(r => new ReviewDto
            {
                ReviewId = r.ReviewId, ReviewerUserId = r.ReviewerUserId, ReviewerName = r.Reviewer?.FullName ?? "",
                ReviewerRole = r.ReviewerRole, Status = r.Status, OverallComment = r.OverallComment, OverallScore = r.OverallScore, SubmittedAt = r.SubmittedAt,
                Items = r.ReviewItems.Select(ri => new ReviewItemDto { ItemId = ri.ItemId, GoalId = ri.GoalId, GoalTitle = ri.Goal?.Title, CompetencyId = ri.CompetencyId, CompetencyName = ri.Competency?.Name, RatingValue = ri.RatingValue, Comment = ri.Comment }).ToList(),
                Scores = r.ReviewScores.Select(rs => new ReviewScoreDto { Id = rs.Id, EvaluationId = rs.EvaluationId, ReviewId = rs.ReviewId, ReviewerId = rs.ReviewerId, PersonalGoalId = rs.PersonalGoalId, GoalTitle = rs.PersonalGoal?.Title ?? "Unknown Goal", ScoreValue = rs.ScoreValue, Comment = rs.Comment, CreatedAt = rs.CreatedAt }).ToList()
            }).ToList(),
            Goals = goalsWithDetails,
            ApprovalHistory = approvalHistory,
            PeerAssignments = evaluation.PeerAssignments.Select(pa => new PeerAssignmentDto { PeerAssignmentId = pa.PeerAssignmentId, PeerUserId = pa.PeerUserId, PeerName = pa.PeerUser?.FullName ?? "" }).ToList(),
            PromotionCase = evaluation.PromotionCases.FirstOrDefault() != null
                ? new PromotionCaseDto { PromotionCaseId = evaluation.PromotionCases.First().PromotionCaseId, RecommendedByHodId = evaluation.PromotionCases.First().RecommendedByHodId, RecommendedByHodName = evaluation.PromotionCases.First().RecommendedByHod?.FullName, RecommendedAt = evaluation.PromotionCases.First().RecommendedAt, GmDecision = evaluation.PromotionCases.First().GmDecision, GmDecidedById = evaluation.PromotionCases.First().GmDecidedById, GmDecidedByName = evaluation.PromotionCases.First().GmDecidedBy?.FullName, GmDecidedAt = evaluation.PromotionCases.First().GmDecidedAt, DecisionReason = evaluation.PromotionCases.First().DecisionReason }
                : null
        };
    }

    public async Task ProcessPromotionDecisionAsync(int evaluationId, int gmUserId, bool approve, string? comment, CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.PromotionCases).Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null) throw new NotFoundException(nameof(Evaluation), evaluationId);
        if (evaluation.Status != STATUS_PENDING_GM_DECISION) throw new BusinessRuleException("Promotion decision can only be made when evaluation is pending GM decision.");

        var userRoles = await GetUserRolesAsync(gmUserId, cancellationToken);
        if (!userRoles.Contains("GM")) throw new BusinessRuleException("Only GM can make promotion decisions.");

        var promotionCase = evaluation.PromotionCases.FirstOrDefault();
        if (promotionCase == null) throw new NotFoundException("Promotion case not found for this evaluation.");

        var oldStatus = evaluation.Status;
        promotionCase.GmDecision = approve ? PromotionDecision.Approved : PromotionDecision.Rejected;
        promotionCase.GmDecidedById = gmUserId;
        promotionCase.GmDecidedAt = DateTime.UtcNow;
        promotionCase.DecisionReason = comment;

        evaluation.Status = approve ? STATUS_PENDING_HR_PROCESSING : STATUS_COMPLETED_PROMOTION_REJECTED;

        _context.Set<ApprovalHistory>().Add(new ApprovalHistory { EvaluationId = evaluationId, ActorUserId = gmUserId, ActorRole = "GM", Action = approve ? "GmApprovedPromotion" : "GmRejectedPromotion", Comment = comment, FromStatus = oldStatus, ToStatus = evaluation.Status, CreatedAt = DateTime.UtcNow });

        if (approve)
        {
            var hrUsers = await _context.Set<UserRole>().Include(ur => ur.Role).Where(ur => ur.Role.Name == "HR").Select(ur => ur.UserId).ToListAsync(cancellationToken);
            foreach (var hrUserId in hrUsers)
            {
                _context.Set<Notification>().Add(new Notification { UserId = hrUserId, Subject = $"Promotion Approved by GM: {evaluation.Employee.FullName}", Channel = "Email", SentAt = DateTime.UtcNow });
                var hr = await _context.Users.FindAsync(new object[] { hrUserId }, cancellationToken);
                if (hr != null) await _emailService.SendEvaluationNotificationAsync(hr.Email, hr.FullName, evaluation.Employee.FullName, "Approved", "HR", $"GM has approved the promotion. Comment: {comment}", evaluationId, cancellationToken);
            }
        }
        else
        {
            _context.Set<Notification>().Add(new Notification { UserId = evaluation.EmployeeId, Subject = "Evaluation Complete", Channel = "Email", SentAt = DateTime.UtcNow });
            await _emailService.SendPromotionNotificationAsync(evaluation.Employee.Email, evaluation.Employee.FullName, evaluation.Employee.FullName, false, comment, cancellationToken);
        }

        _context.Set<AuditLog>().Add(new AuditLog { ActorUserId = gmUserId, EntityType = "PromotionCase", EntityId = promotionCase.PromotionCaseId, Action = approve ? "PROMOTION_APPROVED_GM" : "PROMOTION_REJECTED_GM", BeforeJson = System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus }), AfterJson = System.Text.Json.JsonSerializer.Serialize(new { Status = evaluation.Status }), CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RecommendForPromotionAsync(int evaluationId, int hodUserId, string? comment, CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.Reviews).ThenInclude(r => r.ReviewScores)
            .Include(e => e.Reviews).ThenInclude(r => r.ReviewItems)
            .Include(e => e.PromotionCases).Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null) throw new NotFoundException(nameof(Evaluation), evaluationId);
        if (evaluation.Status != STATUS_PENDING_HOD_REVIEW) throw new BusinessRuleException("Promotion can only be recommended when evaluation is at HOD review stage.");

        var userRoles = await GetUserRolesAsync(hodUserId, cancellationToken);
        if (!userRoles.Contains("HOD")) throw new BusinessRuleException("Only HOD can recommend for promotion.");

        var oldStatus = evaluation.Status;

        // Calculate overall score if not already set
        if (!evaluation.OverallScore.HasValue || evaluation.OverallScore.Value == 0)
        {
            var allReviewScores = evaluation.Reviews.SelectMany(r => r.ReviewScores).ToList();
            if (allReviewScores.Any())
                evaluation.OverallScore = Math.Round(allReviewScores.Average(rs => rs.ScoreValue) * 10m, 2);
            else
            {
                var allReviewItems = evaluation.Reviews.SelectMany(r => r.ReviewItems).ToList();
                if (allReviewItems.Any())
                    evaluation.OverallScore = Math.Round(allReviewItems.Average(ri => ri.RatingValue), 2);
                else
                    evaluation.OverallScore = PROMOTION_THRESHOLD;
            }
        }

        var hodReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.HOD);
        if (hodReview != null) { hodReview.Status = REVIEW_STATUS_APPROVED; hodReview.OverallComment = comment; hodReview.SubmittedAt = DateTime.UtcNow; }

        var promotionCase = evaluation.PromotionCases.FirstOrDefault();
        if (promotionCase == null)
        {
            promotionCase = new PromotionCase { EvaluationId = evaluationId, RecommendedByHodId = hodUserId, RecommendedAt = DateTime.UtcNow, GmDecision = PromotionDecision.Pending };
            _context.Set<PromotionCase>().Add(promotionCase);
        }
        else { promotionCase.RecommendedByHodId = hodUserId; promotionCase.RecommendedAt = DateTime.UtcNow; promotionCase.GmDecision = PromotionDecision.Pending; }

        evaluation.Status = STATUS_PENDING_GM_DECISION;
        _context.Set<ApprovalHistory>().Add(new ApprovalHistory { EvaluationId = evaluationId, ReviewId = hodReview?.ReviewId, ActorUserId = hodUserId, ActorRole = "HOD", Action = "HodRecommendedPromotion", Comment = comment, FromStatus = oldStatus, ToStatus = evaluation.Status, CreatedAt = DateTime.UtcNow });

        var gmUsers = await _context.Set<UserRole>().Include(ur => ur.Role).Where(ur => ur.Role.Name == "GM").Select(ur => ur.UserId).ToListAsync(cancellationToken);
        foreach (var gmUserId in gmUsers)
        {
            _context.Set<Notification>().Add(new Notification { UserId = gmUserId, Subject = $"Promotion Recommendation from HOD: {evaluation.Employee.FullName}", Channel = "Email", SentAt = DateTime.UtcNow });
            var gm = await _context.Users.FindAsync(new object[] { gmUserId }, cancellationToken);
            if (gm != null) await _emailService.SendEvaluationNotificationAsync(gm.Email, gm.FullName, evaluation.Employee.FullName, "Pending", "GM", "HOD has recommended this employee for promotion.", evaluationId, cancellationToken);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAtHodAsync(int evaluationId, int hodUserId, string comment, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(comment)) throw new BusinessRuleException("A comment is required when rejecting at HOD stage.");

        var evaluation = await _context.Set<Evaluation>().Include(e => e.Reviews).Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null) throw new NotFoundException(nameof(Evaluation), evaluationId);
        if (evaluation.Status != STATUS_PENDING_HOD_REVIEW) throw new BusinessRuleException("Evaluation can only be rejected when at HOD review stage.");

        var userRoles = await GetUserRolesAsync(hodUserId, cancellationToken);
        if (!userRoles.Contains("HOD")) throw new BusinessRuleException("Only HOD can reject at this stage.");

        var oldStatus = evaluation.Status;
        var hodReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.HOD);
        if (hodReview != null) { hodReview.Status = REVIEW_STATUS_REJECTED; hodReview.OverallComment = comment; hodReview.SubmittedAt = DateTime.UtcNow; }

        evaluation.Status = STATUS_REJECTED;
        _context.Set<ApprovalHistory>().Add(new ApprovalHistory { EvaluationId = evaluationId, ReviewId = hodReview?.ReviewId, ActorUserId = hodUserId, ActorRole = "HOD", Action = "HodRejected", Comment = comment, FromStatus = oldStatus, ToStatus = evaluation.Status, CreatedAt = DateTime.UtcNow });
        _context.Set<Notification>().Add(new Notification { UserId = evaluation.EmployeeId, Subject = "Evaluation Rejected by HOD", Channel = "Email", SentAt = DateTime.UtcNow });

        var actor = await _context.Users.FindAsync(new object[] { hodUserId }, cancellationToken);
        if (actor != null) await _emailService.SendRejectionNotificationAsync(evaluation.Employee.Email, evaluation.Employee.FullName, evaluation.Employee.FullName, actor.FullName, "HOD", comment, evaluationId, cancellationToken);

        _context.Set<AuditLog>().Add(new AuditLog { ActorUserId = hodUserId, EntityType = "Evaluation", EntityId = evaluationId, Action = "EVALUATION_REJECTED_HOD", BeforeJson = System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus }), AfterJson = System.Text.Json.JsonSerializer.Serialize(new { Status = evaluation.Status, Comment = comment }), CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task FinalizePromotionByHrAsync(int evaluationId, int hrUserId, bool proceed, string? comment, CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Set<Evaluation>().Include(e => e.PromotionCases).Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null) throw new NotFoundException(nameof(Evaluation), evaluationId);
        if (evaluation.Status != STATUS_PENDING_HR_PROCESSING && evaluation.Status != STATUS_V2_PENDING_HR_PROMOTION)
            throw new BusinessRuleException("HR can only process promotion when it's pending HR processing.");

        var userRoles = await GetUserRolesAsync(hrUserId, cancellationToken);
        if (!userRoles.Contains("HR")) throw new BusinessRuleException("Only HR can process promotions.");

        var promotionCase = evaluation.PromotionCases.FirstOrDefault();
        if (promotionCase == null || promotionCase.GmDecision != PromotionDecision.Approved) throw new BusinessRuleException("Promotion case must be approved by GM before HR processing.");

        var oldStatus = evaluation.Status;

        if (proceed)
        {
            evaluation.Status = evaluation.WorkflowVersion == "v2"
                ? STATUS_V2_COMPLETED_PROMOTION_APPROVED
                : STATUS_COMPLETED_PROMOTION_APPROVED;
            _context.Set<ApprovalHistory>().Add(new ApprovalHistory { EvaluationId = evaluationId, ActorUserId = hrUserId, ActorRole = "HR", Action = "HrProcessedPromotion", Comment = comment, FromStatus = oldStatus, ToStatus = evaluation.Status, CreatedAt = DateTime.UtcNow });
            _context.Set<Notification>().Add(new Notification { UserId = evaluation.EmployeeId, Subject = "Congratulations! Your promotion has been processed", Channel = "Email", SentAt = DateTime.UtcNow });
            await _emailService.SendPromotionNotificationAsync(evaluation.Employee.Email, evaluation.Employee.FullName, evaluation.Employee.FullName, true, comment, cancellationToken);
            _context.Set<AuditLog>().Add(new AuditLog { ActorUserId = hrUserId, EntityType = "PromotionCase", EntityId = promotionCase.PromotionCaseId, Action = "PROMOTION_PROCESSED_HR", BeforeJson = System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus }), AfterJson = System.Text.Json.JsonSerializer.Serialize(new { Status = evaluation.Status, Comment = comment }), CreatedAt = DateTime.UtcNow });
        }
        else
        {
            evaluation.Status = evaluation.WorkflowVersion == "v2"
                ? STATUS_V2_COMPLETED_NO_PROMOTION
                : STATUS_COMPLETED_PROMOTION_REJECTED;
            _context.Set<ApprovalHistory>().Add(new ApprovalHistory { EvaluationId = evaluationId, ActorUserId = hrUserId, ActorRole = "HR", Action = "HrDeclinedPromotion", Comment = comment, FromStatus = oldStatus, ToStatus = evaluation.Status, CreatedAt = DateTime.UtcNow });
            _context.Set<Notification>().Add(new Notification { UserId = evaluation.EmployeeId, Subject = "Evaluation Complete", Channel = "Email", SentAt = DateTime.UtcNow });
            await _emailService.SendPromotionNotificationAsync(evaluation.Employee.Email, evaluation.Employee.FullName, evaluation.Employee.FullName, false, comment, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
