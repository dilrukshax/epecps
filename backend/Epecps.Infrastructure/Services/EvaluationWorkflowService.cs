using Epecps.Application.DTOs.Evaluations;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Domain.Enums;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// Implementation of the evaluation approval workflow service
/// Manages the approval matrix: Self ? RM ? TL ? Peer1 ? Peer2 ? HOD ? GM ? HR
/// </summary>
public class EvaluationWorkflowService : IEvaluationWorkflowService
{
    private readonly EpecpsDbContext _context;

    // Evaluation status constants
    private const string STATUS_PENDING_RM_REVIEW = "Pending_RM_Review";
    private const string STATUS_PENDING_TL_REVIEW = "Pending_TL_Review";
    private const string STATUS_PENDING_PEER_ASSIGNMENT = "Pending_Peer_Assignment";
    private const string STATUS_PENDING_PEER_REVIEWS = "Pending_Peer_Reviews";
    private const string STATUS_PENDING_HOD_REVIEW = "Pending_HOD_Review";
    private const string STATUS_PENDING_GM_DECISION = "Pending_GM_Decision";
    private const string STATUS_COMPLETED_NO_PROMOTION = "Completed_NoPromotion";
    private const string STATUS_COMPLETED_PROMOTION_APPROVED = "Completed_PromotionApproved";
    private const string STATUS_COMPLETED_PROMOTION_REJECTED = "Completed_PromotionRejected";
    private const string STATUS_REJECTED = "Rejected";

    // Review status constants
    private const string REVIEW_STATUS_PENDING = "Pending";
    private const string REVIEW_STATUS_APPROVED = "Approved";
    private const string REVIEW_STATUS_REJECTED = "Rejected";
    private const string REVIEW_STATUS_COMPLETED = "Completed";

    // Score threshold for promotion
    private const decimal PROMOTION_THRESHOLD = 80.0m;

    public EvaluationWorkflowService(EpecpsDbContext context)
    {
        _context = context;
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

        // TODO: Determine RM and TL from organizational structure
        // For now, we'll use placeholder logic - in production, this should query org hierarchy
        var reportingManagerId = await GetReportingManagerIdAsync(employeeId, cancellationToken);
        var teamLeadId = await GetTeamLeadIdAsync(employeeId, cancellationToken);

        // Create the evaluation
        var evaluation = new Evaluation
        {
            CycleId = cycleId,
            EmployeeId = employeeId,
            ReportingManagerId = reportingManagerId,
            TeamLeadId = teamLeadId,
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
                Title = personalGoal.Title,
                Description = personalGoal.Description ?? string.Empty,
                WeightPct = 100m / personalGoals.Count, // Distribute weight equally
                EvidenceUri = null // Can be populated later
            };

            _context.Set<EmployeeGoal>().Add(employeeGoal);
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

        // Lock personal goals - update status to prevent editing
        foreach (var goal in personalGoals)
        {
            goal.Status = PersonalGoalStatus.UnderEvaluation;
            goal.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Create approval history entry
        var approvalHistory = new ApprovalHistory
        {
            EvaluationId = evaluation.EvaluationId,
            ReviewId = selfReview.ReviewId,
            ActorUserId = employeeId,
            ActorRole = "Employee",
            Action = "Submitted",
            Comment = "Goal set submitted for evaluation",
            FromStatus = "Draft",
            ToStatus = STATUS_PENDING_RM_REVIEW,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ApprovalHistory>().Add(approvalHistory);

        // Create notification for RM
        var notification = new Notification
        {
            UserId = reportingManagerId,
            Subject = $"New Evaluation Pending: {employee.FullName}",
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
            Action = "EVALUATION_SUBMITTED",
            BeforeJson = null,
            AfterJson = System.Text.Json.JsonSerializer.Serialize(new { evaluation.EvaluationId, evaluation.Status }),
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<AuditLog>().Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        return evaluation;
    }

    public async Task ApproveAsync(int evaluationId, int actorUserId, string? comment, CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.Reviews)
            .Include(e => e.PeerAssignments)
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
                
                currentReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.RM);
                actorRole = "RM";
                await TransitionToTeamLeadReviewAsync(evaluation, cancellationToken);
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
                
                currentReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.Peer && r.ReviewerUserId == actorUserId);
                actorRole = "Peer";
                
                // Check if both peers have approved
                await CheckAndTransitionAfterPeerReviewsAsync(evaluation, cancellationToken);
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

        // Create notification for next approver or employee
        await CreateNextStepNotificationAsync(evaluation, actorUserId, cancellationToken);

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

        switch (evaluation.Status)
        {
            case STATUS_PENDING_RM_REVIEW:
                if (actorUserId != evaluation.ReportingManagerId)
                    throw new BusinessRuleException("Only the Reporting Manager can reject at this stage.");
                currentReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.RM);
                actorRole = "RM";
                break;

            case STATUS_PENDING_TL_REVIEW:
                if (actorUserId != evaluation.TeamLeadId)
                    throw new BusinessRuleException("Only the Team Lead can reject at this stage.");
                currentReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.TL);
                actorRole = "TL";
                break;

            case STATUS_PENDING_PEER_REVIEWS:
                var peerAssignment = evaluation.PeerAssignments.FirstOrDefault(pa => pa.PeerUserId == actorUserId);
                if (peerAssignment == null)
                    throw new BusinessRuleException("Only assigned peer reviewers can reject at this stage.");
                currentReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.Peer && r.ReviewerUserId == actorUserId);
                actorRole = "Peer";
                break;

            case STATUS_PENDING_HOD_REVIEW:
                if (!actorRoles.Contains("HOD"))
                    throw new BusinessRuleException("Only HOD can reject at this stage.");
                currentReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.HOD);
                actorRole = "HOD";
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

        // Update evaluation status
        evaluation.Status = STATUS_REJECTED;

        // Unlock personal goals
        var personalGoals = await _context.PersonalGoals
            .Where(pg => pg.GoalSetId != null && pg.UserId == evaluation.EmployeeId && pg.Status == PersonalGoalStatus.UnderEvaluation)
            .ToListAsync(cancellationToken);

        foreach (var goal in personalGoals)
        {
            goal.Status = PersonalGoalStatus.Completed;
            goal.UpdatedAt = DateTime.UtcNow;
        }

        // Create approval history
        var approvalHistory = new ApprovalHistory
        {
            EvaluationId = evaluationId,
            ReviewId = currentReview?.ReviewId,
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            Action = "Rejected",
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
            Subject = $"Evaluation Rejected by {actorRole}",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        };

        _context.Set<Notification>().Add(notification);

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

        if (peerUserId1 == peerUserId2)
            throw new BusinessRuleException("Peer reviewers must be different users.");

        // Verify peer users exist
        var peer1 = await _context.Users.FindAsync(new object[] { peerUserId1 }, cancellationToken);
        var peer2 = await _context.Users.FindAsync(new object[] { peerUserId2 }, cancellationToken);

        if (peer1 == null || peer2 == null)
            throw new NotFoundException("One or both peer reviewers not found.");

        var oldStatus = evaluation.Status;

        // Create peer assignments
        var peerAssignment1 = new PeerAssignment
        {
            EvaluationId = evaluationId,
            PeerUserId = peerUserId1
        };

        var peerAssignment2 = new PeerAssignment
        {
            EvaluationId = evaluationId,
            PeerUserId = peerUserId2
        };

        _context.Set<PeerAssignment>().Add(peerAssignment1);
        _context.Set<PeerAssignment>().Add(peerAssignment2);

        // Create peer review records
        var peerReview1 = new Review
        {
            EvaluationId = evaluationId,
            ReviewerUserId = peerUserId1,
            ReviewerRole = ReviewerRole.Peer,
            Status = REVIEW_STATUS_PENDING,
            OverallComment = null,
            SubmittedAt = null
        };

        var peerReview2 = new Review
        {
            EvaluationId = evaluationId,
            ReviewerUserId = peerUserId2,
            ReviewerRole = ReviewerRole.Peer,
            Status = REVIEW_STATUS_PENDING,
            OverallComment = null,
            SubmittedAt = null
        };

        _context.Set<Review>().Add(peerReview1);
        _context.Set<Review>().Add(peerReview2);

        // Update evaluation status
        evaluation.Status = STATUS_PENDING_PEER_REVIEWS;

        // Create approval history
        var approvalHistory = new ApprovalHistory
        {
            EvaluationId = evaluationId,
            ReviewId = null,
            ActorUserId = teamLeadUserId,
            ActorRole = "TL",
            Action = "AssignedPeers",
            Comment = $"Assigned peer reviewers: {peer1.FullName}, {peer2.FullName}",
            FromStatus = oldStatus,
            ToStatus = evaluation.Status,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ApprovalHistory>().Add(approvalHistory);

        // Notify both peers
        var notification1 = new Notification
        {
            UserId = peerUserId1,
            Subject = $"Peer Review Request: {evaluation.Employee?.FullName ?? "Employee"}",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        };

        var notification2 = new Notification
        {
            UserId = peerUserId2,
            Subject = $"Peer Review Request: {evaluation.Employee?.FullName ?? "Employee"}",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        };

        _context.Set<Notification>().Add(notification1);
        _context.Set<Notification>().Add(notification2);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<PendingApprovalDto>> GetPendingApprovalsForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        var pendingApprovals = new List<PendingApprovalDto>();

        // Get evaluations where user is RM and status is pending RM review
        var rmApprovals = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Cycle)
            .Where(e => e.ReportingManagerId == userId && e.Status == STATUS_PENDING_RM_REVIEW)
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

        pendingApprovals.AddRange(rmApprovals);

        // Get evaluations where user is TL and status is pending TL review
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

        // Get evaluations where user is assigned as peer and status is pending peer reviews
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

        // Get evaluations where user is HOD and status is pending HOD review
        if (userRoles.Contains("HOD"))
        {
            var hodApprovals = await _context.Set<Evaluation>()
                .Include(e => e.Employee)
                .Include(e => e.Cycle)
                .Where(e => e.Status == STATUS_PENDING_HOD_REVIEW)
                .Select(e => new PendingApprovalDto
                {
                    EvaluationId = e.EvaluationId,
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.Employee.FullName,
                    Status = e.Status,
                    RequiredRole = "HOD",
                    SubmittedDate = e.Reviews
                        .Where(r => r.ReviewerRole == ReviewerRole.Peer)
                        .OrderByDescending(r => r.SubmittedAt)
                        .Select(r => r.SubmittedAt)
                        .FirstOrDefault(),
                    CycleId = e.CycleId,
                    CycleName = e.Cycle.Name
                })
                .ToListAsync(cancellationToken);

            pendingApprovals.AddRange(hodApprovals);
        }

        // Get evaluations where user is GM and status is pending GM decision
        if (userRoles.Contains("GM"))
        {
            var gmApprovals = await _context.Set<Evaluation>()
                .Include(e => e.Employee)
                .Include(e => e.Cycle)
                .Where(e => e.Status == STATUS_PENDING_GM_DECISION)
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

        return pendingApprovals.OrderByDescending(p => p.SubmittedDate);
    }

    public async Task<EvaluationDetailDto> GetEvaluationDetailsAsync(int evaluationId, int userId, CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.ReportingManager)
            .Include(e => e.TeamLead)
            .Include(e => e.Cycle)
            .Include(e => e.Reviews)
                .ThenInclude(r => r.Reviewer)
            .Include(e => e.Reviews)
                .ThenInclude(r => r.ReviewItems)
                    .ThenInclude(ri => ri.Goal)
            .Include(e => e.Reviews)
                .ThenInclude(r => r.ReviewItems)
                    .ThenInclude(ri => ri.Competency)
            .Include(e => e.EmployeeGoals)
            .Include(e => e.PeerAssignments)
                .ThenInclude(pa => pa.PeerUser)
            .Include(e => e.PromotionCases)
                .ThenInclude(pc => pc.RecommendedByHod)
            .Include(e => e.PromotionCases)
                .ThenInclude(pc => pc.GmDecidedBy)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
            throw new NotFoundException(nameof(Evaluation), evaluationId);

        // Authorization check - user must be involved in the evaluation
        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        var isAuthorized = userId == evaluation.EmployeeId ||
                          userId == evaluation.ReportingManagerId ||
                          userId == evaluation.TeamLeadId ||
                          evaluation.PeerAssignments.Any(pa => pa.PeerUserId == userId) ||
                          userRoles.Contains("HOD") ||
                          userRoles.Contains("GM") ||
                          userRoles.Contains("HR") ||
                          userRoles.Contains("Admin");

        if (!isAuthorized)
            throw new BusinessRuleException("You do not have permission to view this evaluation.");

        // Get approval history
        var approvalHistory = await _context.Set<ApprovalHistory>()
            .Include(ah => ah.ActorUser)
            .Where(ah => ah.EvaluationId == evaluationId)
            .OrderBy(ah => ah.CreatedAt)
            .Select(ah => new ApprovalHistoryItemDto
            {
                Id = ah.Id,
                ActorUserId = ah.ActorUserId,
                ActorName = ah.ActorUser.FullName,
                ActorRole = ah.ActorRole,
                Action = ah.Action,
                Comment = ah.Comment,
                FromStatus = ah.FromStatus,
                ToStatus = ah.ToStatus,
                CreatedAt = ah.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var dto = new EvaluationDetailDto
        {
            EvaluationId = evaluation.EvaluationId,
            CycleId = evaluation.CycleId,
            CycleName = evaluation.Cycle.Name,
            EmployeeId = evaluation.EmployeeId,
            EmployeeName = evaluation.Employee.FullName,
            EmployeeEmail = evaluation.Employee.Email,
            ReportingManagerId = evaluation.ReportingManagerId,
            ReportingManagerName = evaluation.ReportingManager.FullName,
            TeamLeadId = evaluation.TeamLeadId,
            TeamLeadName = evaluation.TeamLead.FullName,
            Status = evaluation.Status,
            OverallScore = evaluation.OverallScore,
            Reviews = evaluation.Reviews.Select(r => new ReviewDto
            {
                ReviewId = r.ReviewId,
                ReviewerUserId = r.ReviewerUserId,
                ReviewerName = r.Reviewer.FullName,
                ReviewerRole = r.ReviewerRole,
                Status = r.Status,
                OverallComment = r.OverallComment,
                SubmittedAt = r.SubmittedAt,
                Items = r.ReviewItems.Select(ri => new ReviewItemDto
                {
                    ItemId = ri.ItemId,
                    GoalId = ri.GoalId,
                    GoalTitle = ri.Goal?.Title,
                    CompetencyId = ri.CompetencyId,
                    CompetencyName = ri.Competency?.Name,
                    RatingValue = ri.RatingValue,
                    Comment = ri.Comment
                }).ToList()
            }).ToList(),
            Goals = evaluation.EmployeeGoals.Select(g => new GoalDto
            {
                GoalId = g.GoalId,
                Title = g.Title,
                Description = g.Description,
                WeightPct = g.WeightPct,
                EvidenceUri = g.EvidenceUri
            }).ToList(),
            ApprovalHistory = approvalHistory,
            PeerAssignments = evaluation.PeerAssignments.Select(pa => new PeerAssignmentDto
            {
                PeerAssignmentId = pa.PeerAssignmentId,
                PeerUserId = pa.PeerUserId,
                PeerName = pa.PeerUser.FullName
            }).ToList(),
            PromotionCase = evaluation.PromotionCases.FirstOrDefault() != null
                ? new PromotionCaseDto
                {
                    PromotionCaseId = evaluation.PromotionCases.First().PromotionCaseId,
                    RecommendedByHodId = evaluation.PromotionCases.First().RecommendedByHodId,
                    RecommendedByHodName = evaluation.PromotionCases.First().RecommendedByHod?.FullName,
                    RecommendedAt = evaluation.PromotionCases.First().RecommendedAt,
                    GmDecision = evaluation.PromotionCases.First().GmDecision,
                    GmDecidedById = evaluation.PromotionCases.First().GmDecidedById,
                    GmDecidedByName = evaluation.PromotionCases.First().GmDecidedBy?.FullName,
                    GmDecidedAt = evaluation.PromotionCases.First().GmDecidedAt,
                    DecisionReason = evaluation.PromotionCases.First().DecisionReason
                }
                : null
        };

        return dto;
    }

    public async Task ProcessPromotionDecisionAsync(int evaluationId, int gmUserId, bool approve, string? comment, CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.PromotionCases)
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
            throw new NotFoundException(nameof(Evaluation), evaluationId);

        if (evaluation.Status != STATUS_PENDING_GM_DECISION)
            throw new BusinessRuleException("Promotion decision can only be made when evaluation is pending GM decision.");

        var userRoles = await GetUserRolesAsync(gmUserId, cancellationToken);
        if (!userRoles.Contains("GM"))
            throw new BusinessRuleException("Only GM can make promotion decisions.");

        var promotionCase = evaluation.PromotionCases.FirstOrDefault();
        if (promotionCase == null)
            throw new NotFoundException("Promotion case not found for this evaluation.");

        var oldStatus = evaluation.Status;

        // Update promotion case
        promotionCase.GmDecision = approve ? PromotionDecision.Approved : PromotionDecision.Rejected;
        promotionCase.GmDecidedById = gmUserId;
        promotionCase.GmDecidedAt = DateTime.UtcNow;
        promotionCase.DecisionReason = comment;

        // Update evaluation status
        evaluation.Status = approve ? STATUS_COMPLETED_PROMOTION_APPROVED : STATUS_COMPLETED_PROMOTION_REJECTED;

        // Create approval history
        var approvalHistory = new ApprovalHistory
        {
            EvaluationId = evaluationId,
            ReviewId = null,
            ActorUserId = gmUserId,
            ActorRole = "GM",
            Action = approve ? "GmApprovedPromotion" : "GmRejectedPromotion",
            Comment = comment,
            FromStatus = oldStatus,
            ToStatus = evaluation.Status,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ApprovalHistory>().Add(approvalHistory);

        // Notify HR if approved
        if (approve)
        {
            // Get HR users
            var hrUsers = await _context.Set<UserRole>()
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.Name == "HR")
                .Select(ur => ur.UserId)
                .ToListAsync(cancellationToken);

            foreach (var hrUserId in hrUsers)
            {
                var hrNotification = new Notification
                {
                    UserId = hrUserId,
                    Subject = $"Promotion Approved: {evaluation.Employee.FullName}",
                    Channel = "Email",
                    SentAt = DateTime.UtcNow
                };

                _context.Set<Notification>().Add(hrNotification);
            }
        }

        // Notify employee
        var employeeNotification = new Notification
        {
            UserId = evaluation.EmployeeId,
            Subject = approve 
                ? "Congratulations! Your promotion has been approved" 
                : "Evaluation Complete - Continue your excellent work!",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        };

        _context.Set<Notification>().Add(employeeNotification);

        // Create audit log
        var auditLog = new AuditLog
        {
            ActorUserId = gmUserId,
            EntityType = "PromotionCase",
            EntityId = promotionCase.PromotionCaseId,
            Action = approve ? "PROMOTION_APPROVED_GM" : "PROMOTION_REJECTED_GM",
            BeforeJson = System.Text.Json.JsonSerializer.Serialize(new { Decision = PromotionDecision.Pending }),
            AfterJson = System.Text.Json.JsonSerializer.Serialize(new { promotionCase.GmDecision, Comment = comment }),
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<AuditLog>().Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);
    }

    #region Private Helper Methods

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

        await Task.CompletedTask;
    }

    private async Task CheckAndTransitionAfterPeerReviewsAsync(Evaluation evaluation, CancellationToken cancellationToken)
    {
        // Check if both peer reviews are approved
        var peerReviews = evaluation.Reviews.Where(r => r.ReviewerRole == ReviewerRole.Peer).ToList();
        
        if (peerReviews.Count == 2 && peerReviews.All(r => r.Status == REVIEW_STATUS_APPROVED))
        {
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
                Subject = $"Evaluation Pending: {evaluation.Employee?.FullName ?? "Employee"}",
                Channel = "Email",
                SentAt = DateTime.UtcNow
            };

            _context.Set<Notification>().Add(notification);
        }

        await Task.CompletedTask;
    }

    private async Task TransitionAfterHodReviewAsync(Evaluation evaluation, int hodUserId, CancellationToken cancellationToken)
    {
        // Calculate overall score from all review items
        var allReviewItems = await _context.Set<ReviewItem>()
            .Where(ri => evaluation.Reviews.Select(r => r.ReviewId).Contains(ri.ReviewId))
            .ToListAsync(cancellationToken);

        if (allReviewItems.Any())
        {
            evaluation.OverallScore = Math.Round(allReviewItems.Average(ri => ri.RatingValue), 2);
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

    #endregion
}
