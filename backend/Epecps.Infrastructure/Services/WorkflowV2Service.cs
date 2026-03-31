using Epecps.Application.DTOs.WorkflowV2;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Domain.Enums;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

public class WorkflowV2Service : IWorkflowV2Service
{
    private readonly EpecpsDbContext _context;

    public WorkflowV2Service(EpecpsDbContext context)
    {
        _context = context;
    }

    public async Task SubmitActivationPlanAsync(
        Guid goalSetId,
        int employeeUserId,
        SubmitActivationPlanRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Evaluations
            .FirstOrDefaultAsync(e =>
                e.GoalSetId == goalSetId &&
                e.EmployeeId == employeeUserId &&
                e.WorkflowVersion == "v2",
                cancellationToken);

        if (evaluation == null)
        {
            throw new NotFoundException("Evaluation for this goal set was not found.");
        }

        if (evaluation.Status != "V2_PENDING_EMPLOYEE_ACTIVATION" && evaluation.Status != "V2_RETURNED_FOR_ACTIVATION")
        {
            throw new BusinessRuleException($"Activation plan cannot be submitted in current status: {evaluation.Status}");
        }

        var assignments = await _context.GoalAssignments
            .Where(a => a.GoalSetId == goalSetId && a.AssignedToUserId == employeeUserId)
            .ToListAsync(cancellationToken);

        if (assignments.Count < 5)
        {
            throw new BusinessRuleException("At least 5 goals are required for activation plan submission.");
        }

        if (request.Goals.Count != assignments.Count)
        {
            throw new BusinessRuleException("Activation method is required for each assigned goal.");
        }

        var requestByGoalId = request.Goals.ToDictionary(x => x.GoalAssignmentId);

        foreach (var assignment in assignments)
        {
            if (!requestByGoalId.TryGetValue(assignment.Id, out var item) || string.IsNullOrWhiteSpace(item.Method))
            {
                throw new BusinessRuleException($"Activation method is missing for assignment {assignment.Id}.");
            }

            assignment.ActivationMethod = item.Method.Trim();
            assignment.ActivationSubmittedAt = DateTime.UtcNow;
            assignment.ActivationStatus = "PendingTL";
            assignment.ActivationTlComment = null;
            assignment.ActivationReviewedByUserId = null;
            assignment.ActivationReviewedAt = null;
            assignment.UpdatedAt = DateTime.UtcNow;
        }

        var fromStatus = evaluation.Status;
        evaluation.Status = "V2_PENDING_TL_ACTIVATION_REVIEW";

        _context.ApprovalHistories.Add(new ApprovalHistory
        {
            EvaluationId = evaluation.EvaluationId,
            ActorUserId = employeeUserId,
            ActorRole = "Employee",
            Action = "SubmittedActivationPlan",
            Comment = "Employee submitted goal activation methods.",
            FromStatus = fromStatus,
            ToStatus = evaluation.Status,
            CreatedAt = DateTime.UtcNow
        });

        _context.Notifications.Add(new Notification
        {
            UserId = evaluation.TeamLeadId,
            Subject = "Activation plan pending TL review",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessActivationDecisionAsync(
        int evaluationId,
        int tlUserId,
        ActivationPlanDecisionDto request,
        CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Evaluations
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
        {
            throw new NotFoundException(nameof(Evaluation), evaluationId);
        }

        if (evaluation.WorkflowVersion != "v2")
        {
            throw new BusinessRuleException("Activation decision is only available for workflow v2.");
        }

        if (evaluation.TeamLeadId != tlUserId)
        {
            throw new BusinessRuleException("Only assigned Team Lead can review activation plans.");
        }

        if (evaluation.Status != "V2_PENDING_TL_ACTIVATION_REVIEW")
        {
            throw new BusinessRuleException($"Activation decision cannot be processed in status: {evaluation.Status}");
        }

        var assignments = await _context.GoalAssignments
            .Where(a => a.GoalSetId == evaluation.GoalSetId && a.AssignedToUserId == evaluation.EmployeeId)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var fromStatus = evaluation.Status;

        if (request.Approved)
        {
            foreach (var assignment in assignments)
            {
                assignment.ActivationStatus = "Approved";
                assignment.ActivationTlComment = request.Comment;
                assignment.ActivationReviewedByUserId = tlUserId;
                assignment.ActivationReviewedAt = now;
                assignment.UpdatedAt = now;
            }

            evaluation.Status = "V2_ACTIVE_GOALS";
        }
        else
        {
            var rejectedSet = request.RejectedGoalAssignmentIds?.ToHashSet() ?? new HashSet<Guid>();
            if (rejectedSet.Count == 0)
            {
                rejectedSet = assignments.Select(a => a.Id).ToHashSet();
            }

            foreach (var assignment in assignments)
            {
                if (rejectedSet.Contains(assignment.Id))
                {
                    assignment.ActivationStatus = "Rejected";
                    assignment.ActivationTlComment = request.Comment ?? "Please update activation plan.";
                    assignment.ActivationReviewedByUserId = tlUserId;
                    assignment.ActivationReviewedAt = now;
                    assignment.UpdatedAt = now;
                }
            }

            evaluation.Status = "V2_RETURNED_FOR_ACTIVATION";
        }

        _context.ApprovalHistories.Add(new ApprovalHistory
        {
            EvaluationId = evaluation.EvaluationId,
            ActorUserId = tlUserId,
            ActorRole = "TL",
            Action = request.Approved ? "ActivationApprovedByTL" : "ActivationRejectedByTL",
            Comment = request.Comment,
            FromStatus = fromStatus,
            ToStatus = evaluation.Status,
            CreatedAt = now
        });

        _context.Notifications.Add(new Notification
        {
            UserId = evaluation.EmployeeId,
            Subject = request.Approved ? "Activation plan approved by TL" : "Activation plan returned by TL",
            Channel = "Email",
            SentAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitSelfEvaluationAsync(
        int evaluationId,
        int employeeUserId,
        SubmitSelfEvaluationV2Dto request,
        CancellationToken cancellationToken = default)
    {
        if (request.SelfScore < 0 || request.SelfScore > 100)
        {
            throw new BusinessRuleException("Self score must be between 0 and 100.");
        }

        if (request.PeerUserId1 == request.PeerUserId2)
        {
            throw new BusinessRuleException("Peer reviewers must be two different users.");
        }

        var evaluation = await _context.Evaluations
            .Include(e => e.Reviews)
            .Include(e => e.PeerAssignments)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
        {
            throw new NotFoundException(nameof(Evaluation), evaluationId);
        }

        if (evaluation.WorkflowVersion != "v2")
        {
            throw new BusinessRuleException("Self-evaluation submission is only available for workflow v2.");
        }

        if (evaluation.EmployeeId != employeeUserId)
        {
            throw new BusinessRuleException("Only assigned employee can submit self-evaluation.");
        }

        if (evaluation.Status != "V2_ACTIVE_GOALS")
        {
            throw new BusinessRuleException($"Self-evaluation cannot be submitted in status: {evaluation.Status}");
        }

        var selfReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.Self && r.ReviewerUserId == employeeUserId);
        if (selfReview == null)
        {
            selfReview = new Review
            {
                EvaluationId = evaluation.EvaluationId,
                ReviewerUserId = employeeUserId,
                ReviewerRole = ReviewerRole.Self
            };
            _context.Reviews.Add(selfReview);
        }

        selfReview.Status = "Completed";
        selfReview.OverallScore = request.SelfScore;
        selfReview.OverallComment = request.Comment;
        selfReview.SubmittedAt = DateTime.UtcNow;

        await EnsurePendingReviewAsync(evaluation.EvaluationId, evaluation.ReportingManagerId, ReviewerRole.RM, cancellationToken);
        await EnsurePendingReviewAsync(evaluation.EvaluationId, evaluation.TeamLeadId, ReviewerRole.TL, cancellationToken);

        var existingPeerAssignments = await _context.PeerAssignments
            .Where(pa => pa.EvaluationId == evaluation.EvaluationId)
            .ToListAsync(cancellationToken);
        if (existingPeerAssignments.Count > 0)
        {
            _context.PeerAssignments.RemoveRange(existingPeerAssignments);
        }

        var existingPeerReviews = await _context.Reviews
            .Where(r => r.EvaluationId == evaluation.EvaluationId && r.ReviewerRole == ReviewerRole.Peer)
            .ToListAsync(cancellationToken);
        if (existingPeerReviews.Count > 0)
        {
            _context.Reviews.RemoveRange(existingPeerReviews);
        }

        _context.PeerAssignments.Add(new PeerAssignment
        {
            EvaluationId = evaluation.EvaluationId,
            PeerUserId = request.PeerUserId1
        });
        _context.PeerAssignments.Add(new PeerAssignment
        {
            EvaluationId = evaluation.EvaluationId,
            PeerUserId = request.PeerUserId2
        });

        _context.Reviews.Add(new Review
        {
            EvaluationId = evaluation.EvaluationId,
            ReviewerUserId = request.PeerUserId1,
            ReviewerRole = ReviewerRole.Peer,
            Status = "Pending"
        });
        _context.Reviews.Add(new Review
        {
            EvaluationId = evaluation.EvaluationId,
            ReviewerUserId = request.PeerUserId2,
            ReviewerRole = ReviewerRole.Peer,
            Status = "Pending"
        });

        var fromStatus = evaluation.Status;
        evaluation.Status = "V2_PENDING_PARALLEL_REVIEWS";

        _context.ApprovalHistories.Add(new ApprovalHistory
        {
            EvaluationId = evaluation.EvaluationId,
            ActorUserId = employeeUserId,
            ActorRole = "Employee",
            Action = "SubmittedSelfEvaluationAndStartedParallelReviews",
            Comment = request.Comment,
            FromStatus = fromStatus,
            ToStatus = evaluation.Status,
            CreatedAt = DateTime.UtcNow
        });

        _context.Notifications.Add(new Notification
        {
            UserId = evaluation.TeamLeadId,
            Subject = "Parallel review requested: TL",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        });
        _context.Notifications.Add(new Notification
        {
            UserId = evaluation.ReportingManagerId,
            Subject = "Parallel review requested: RM",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        });
        _context.Notifications.Add(new Notification
        {
            UserId = request.PeerUserId1,
            Subject = "Parallel review requested: Peer",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        });
        _context.Notifications.Add(new Notification
        {
            UserId = request.PeerUserId2,
            Subject = "Parallel review requested: Peer",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task TryAdvanceAfterParallelReviewAsync(int evaluationId, CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Reviews)
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null || evaluation.WorkflowVersion != "v2" || evaluation.Status != "V2_PENDING_PARALLEL_REVIEWS")
        {
            return;
        }

        var isCompleted = (Review review) => review.Status == "Completed" || review.Status == "Approved";

        var tlReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.TL && r.ReviewerUserId == evaluation.TeamLeadId);
        var rmReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.RM && r.ReviewerUserId == evaluation.ReportingManagerId);
        var peerReviews = evaluation.Reviews
            .Where(r => r.ReviewerRole == ReviewerRole.Peer)
            .OrderBy(r => r.SubmittedAt ?? DateTime.MaxValue)
            .ThenBy(r => r.ReviewId)
            .ToList();

        if (tlReview == null || rmReview == null || peerReviews.Count < 2)
        {
            return;
        }

        if (!isCompleted(tlReview) || !isCompleted(rmReview) || !peerReviews.Take(2).All(isCompleted))
        {
            return;
        }

        var hodUserId = await ResolveHodUserForEmployeeAsync(evaluation.EmployeeId, cancellationToken);
        var existingHodReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.HOD);
        if (existingHodReview == null)
        {
            _context.Reviews.Add(new Review
            {
                EvaluationId = evaluation.EvaluationId,
                ReviewerUserId = hodUserId,
                ReviewerRole = ReviewerRole.HOD,
                Status = "Pending"
            });
        }

        var fromStatus = evaluation.Status;
        evaluation.Status = "V2_PENDING_HOD_REVIEW";

        _context.ApprovalHistories.Add(new ApprovalHistory
        {
            EvaluationId = evaluation.EvaluationId,
            ActorUserId = 0,
            ActorRole = "System",
            Action = "ParallelReviewsCompleted",
            Comment = "TL, RM, and both peer reviews are complete.",
            FromStatus = fromStatus,
            ToStatus = evaluation.Status,
            CreatedAt = DateTime.UtcNow
        });

        _context.Notifications.Add(new Notification
        {
            UserId = hodUserId,
            Subject = "Evaluation pending HOD finalization",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task HODFinalizeAsync(
        int evaluationId,
        int hodUserId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Reviews)
            .Include(e => e.Employee)
            .Include(e => e.PromotionCases)
            .Include(e => e.PipCases)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
        {
            throw new NotFoundException(nameof(Evaluation), evaluationId);
        }

        if (evaluation.WorkflowVersion != "v2")
        {
            throw new BusinessRuleException("HOD finalization is only available for workflow v2.");
        }

        if (evaluation.Status != "V2_PENDING_HOD_REVIEW")
        {
            throw new BusinessRuleException($"HOD finalization cannot be done in status: {evaluation.Status}");
        }

        var allowedHodUserId = await ResolveHodUserForEmployeeAsync(evaluation.EmployeeId, cancellationToken);
        if (allowedHodUserId != hodUserId)
        {
            throw new BusinessRuleException("Only mapped HOD can finalize this evaluation.");
        }

        var finalScore = await CalculateFinalWeightedScoreAsync(evaluation.EvaluationId, cancellationToken);
        evaluation.OverallScore = finalScore;

        var hodReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.HOD);
        if (hodReview == null)
        {
            hodReview = new Review
            {
                EvaluationId = evaluation.EvaluationId,
                ReviewerUserId = hodUserId,
                ReviewerRole = ReviewerRole.HOD
            };
            _context.Reviews.Add(hodReview);
        }

        hodReview.Status = "Approved";
        hodReview.OverallComment = comment;
        hodReview.OverallScore = finalScore;
        hodReview.SubmittedAt = DateTime.UtcNow;

        var fromStatus = evaluation.Status;

        if (finalScore >= 80m)
        {
            evaluation.Status = "V2_PENDING_GM_DECISION";

            var promotionCase = evaluation.PromotionCases.FirstOrDefault();
            if (promotionCase == null)
            {
                promotionCase = new PromotionCase
                {
                    EvaluationId = evaluation.EvaluationId
                };
                _context.PromotionCases.Add(promotionCase);
            }

            promotionCase.RecommendedByHodId = hodUserId;
            promotionCase.RecommendedAt = DateTime.UtcNow;
            promotionCase.GmDecision = PromotionDecision.Pending;
            promotionCase.DecisionReason = comment;
        }
        else
        {
            evaluation.Status = "V2_PENDING_HR_LOW_PERFORMER";
            await CreateOrUpdateLowPerformerPipCaseAsync(evaluation, comment, cancellationToken);
        }

        _context.ApprovalHistories.Add(new ApprovalHistory
        {
            EvaluationId = evaluation.EvaluationId,
            ReviewId = hodReview.ReviewId == 0 ? null : hodReview.ReviewId,
            ActorUserId = hodUserId,
            ActorRole = "HOD",
            Action = "HodFinalizedV2",
            Comment = comment,
            FromStatus = fromStatus,
            ToStatus = evaluation.Status,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task GmDecisionAsync(
        int evaluationId,
        int gmUserId,
        GmV2DecisionDto request,
        CancellationToken cancellationToken = default)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.PromotionCases)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
        {
            throw new NotFoundException(nameof(Evaluation), evaluationId);
        }

        if (evaluation.WorkflowVersion != "v2")
        {
            throw new BusinessRuleException("GM decision endpoint is only for workflow v2.");
        }

        if (evaluation.Status != "V2_PENDING_GM_DECISION")
        {
            throw new BusinessRuleException($"GM decision cannot be processed in status: {evaluation.Status}");
        }

        var gmHasRole = await _context.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == gmUserId && ur.Role.Name == "GM", cancellationToken);

        if (!gmHasRole)
        {
            throw new BusinessRuleException("Only GM can process this decision.");
        }

        var promotionCase = evaluation.PromotionCases.FirstOrDefault();
        if (promotionCase == null)
        {
            promotionCase = new PromotionCase
            {
                EvaluationId = evaluation.EvaluationId
            };
            _context.PromotionCases.Add(promotionCase);
        }

        var fromStatus = evaluation.Status;

        if (request.Approve && request.VacancyAvailable)
        {
            evaluation.Status = "V2_PENDING_HR_PROMOTION";
            promotionCase.GmDecision = PromotionDecision.Approved;
        }
        else if (request.Approve && !request.VacancyAvailable)
        {
            evaluation.Status = "V2_PROMOTION_DEFERRED";
            promotionCase.GmDecision = PromotionDecision.Pending;
        }
        else
        {
            evaluation.Status = "V2_COMPLETED_NO_PROMOTION";
            promotionCase.GmDecision = PromotionDecision.Rejected;
        }

        promotionCase.GmDecidedById = gmUserId;
        promotionCase.GmDecidedAt = DateTime.UtcNow;
        promotionCase.DecisionReason = request.Comment ?? (request.VacancyAvailable ? null : "Deferred due to vacancy unavailability.");

        _context.ApprovalHistories.Add(new ApprovalHistory
        {
            EvaluationId = evaluation.EvaluationId,
            ActorUserId = gmUserId,
            ActorRole = "GM",
            Action = "GmDecisionV2",
            Comment = request.Comment,
            FromStatus = fromStatus,
            ToStatus = evaluation.Status,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<WorkflowReviewWeightDto>> GetReviewWeightsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultWeightsAsync(cancellationToken);

        return await _context.WorkflowReviewWeights
            .OrderBy(w => w.WorkflowReviewWeightId)
            .Select(w => new WorkflowReviewWeightDto
            {
                ReviewerKey = w.ReviewerKey,
                WeightPercent = w.WeightPercent
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorkflowReviewWeightDto>> UpdateReviewWeightsAsync(
        UpdateWorkflowReviewWeightsDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Weights == null || request.Weights.Count == 0)
        {
            throw new BusinessRuleException("At least one weight entry is required.");
        }

        var requiredKeys = new[] { "Self", "TL", "RM", "Peer1", "Peer2" };
        var suppliedKeys = request.Weights.Select(w => w.ReviewerKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (requiredKeys.Any(k => !suppliedKeys.Contains(k)))
        {
            throw new BusinessRuleException("Weights must include Self, TL, RM, Peer1, and Peer2.");
        }

        var total = request.Weights.Sum(w => w.WeightPercent);
        if (Math.Abs(total - 100m) > 0.001m)
        {
            throw new BusinessRuleException("Total weight must equal 100.");
        }

        await EnsureDefaultWeightsAsync(cancellationToken);

        var existing = await _context.WorkflowReviewWeights.ToListAsync(cancellationToken);
        foreach (var item in request.Weights)
        {
            var row = existing.FirstOrDefault(x => x.ReviewerKey.Equals(item.ReviewerKey, StringComparison.OrdinalIgnoreCase));
            if (row == null)
            {
                row = new WorkflowReviewWeight
                {
                    ReviewerKey = item.ReviewerKey
                };
                _context.WorkflowReviewWeights.Add(row);
            }

            row.WeightPercent = item.WeightPercent;
            row.IsActive = true;
            row.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await GetReviewWeightsAsync(cancellationToken);
    }

    public async Task<List<PipCaseDto>> GetPipCasesAsync(
        int? assignedHrUserId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PipCases
            .Include(c => c.EmployeeUser)
            .Include(c => c.AssignedHrUser)
            .Include(c => c.ActionItems)
            .AsQueryable();

        if (assignedHrUserId.HasValue)
        {
            query = query.Where(c => c.AssignedHrUserId == assignedHrUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        var cases = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return cases.Select(MapPipCase).ToList();
    }

    public async Task<PipCaseDto> AddPipActionItemAsync(
        int pipCaseId,
        PipActionItemCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var pipCase = await _context.PipCases
            .Include(c => c.EmployeeUser)
            .Include(c => c.AssignedHrUser)
            .Include(c => c.ActionItems)
            .FirstOrDefaultAsync(c => c.PipCaseId == pipCaseId, cancellationToken);

        if (pipCase == null)
        {
            throw new NotFoundException(nameof(PipCase), pipCaseId);
        }

        var action = new PipActionItem
        {
            PipCaseId = pipCaseId,
            Title = request.Title?.Trim() ?? string.Empty,
            Description = request.Description?.Trim(),
            TrainingMaterialId = request.TrainingMaterialId,
            ExternalTrainingLink = request.ExternalTrainingLink?.Trim(),
            DueDate = request.DueDate,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.PipActionItems.Add(action);
        await _context.SaveChangesAsync(cancellationToken);

        var refreshed = await _context.PipCases
            .Include(c => c.EmployeeUser)
            .Include(c => c.AssignedHrUser)
            .Include(c => c.ActionItems)
            .FirstAsync(c => c.PipCaseId == pipCaseId, cancellationToken);

        return MapPipCase(refreshed);
    }

    public async Task<PipCaseDto> UpdatePipCaseAsync(
        int pipCaseId,
        PipCaseUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var pipCase = await _context.PipCases
            .Include(c => c.EmployeeUser)
            .Include(c => c.AssignedHrUser)
            .Include(c => c.ActionItems)
            .FirstOrDefaultAsync(c => c.PipCaseId == pipCaseId, cancellationToken);

        if (pipCase == null)
        {
            throw new NotFoundException(nameof(PipCase), pipCaseId);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            pipCase.Status = request.Status;
            if (request.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase))
            {
                pipCase.ClosedAt = DateTime.UtcNow;
            }
        }

        if (request.DueDate.HasValue)
        {
            pipCase.DueDate = request.DueDate;
        }

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            pipCase.Reason = request.Reason;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapPipCase(pipCase);
    }

    public async Task<PipCaseDto> UpdatePipActionItemAsync(
        int pipActionItemId,
        PipActionItemUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var actionItem = await _context.PipActionItems
            .Include(a => a.PipCase)
                .ThenInclude(c => c.EmployeeUser)
            .Include(a => a.PipCase)
                .ThenInclude(c => c.AssignedHrUser)
            .Include(a => a.PipCase)
                .ThenInclude(c => c.ActionItems)
            .FirstOrDefaultAsync(a => a.PipActionItemId == pipActionItemId, cancellationToken);

        if (actionItem == null)
        {
            throw new NotFoundException(nameof(PipActionItem), pipActionItemId);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            actionItem.Status = request.Status;
            if (request.Status.Equals("Done", StringComparison.OrdinalIgnoreCase))
            {
                actionItem.CompletedAt = DateTime.UtcNow;
            }
        }

        if (request.DueDate.HasValue)
        {
            actionItem.DueDate = request.DueDate;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapPipCase(actionItem.PipCase);
    }

    private async Task EnsurePendingReviewAsync(
        int evaluationId,
        int reviewerUserId,
        ReviewerRole reviewerRole,
        CancellationToken cancellationToken)
    {
        var existing = await _context.Reviews
            .FirstOrDefaultAsync(
                r => r.EvaluationId == evaluationId &&
                     r.ReviewerUserId == reviewerUserId &&
                     r.ReviewerRole == reviewerRole,
                cancellationToken);

        if (existing != null)
        {
            if (existing.Status != "Completed" && existing.Status != "Approved")
            {
                existing.Status = "Pending";
                existing.SubmittedAt = null;
            }
            return;
        }

        _context.Reviews.Add(new Review
        {
            EvaluationId = evaluationId,
            ReviewerUserId = reviewerUserId,
            ReviewerRole = reviewerRole,
            Status = "Pending"
        });
    }

    private async Task<int> ResolveHodUserForEmployeeAsync(int employeeUserId, CancellationToken cancellationToken)
    {
        var employeeDeptId = await _context.Users
            .Where(u => u.UserId == employeeUserId)
            .Select(u => u.DeptId)
            .FirstOrDefaultAsync(cancellationToken);

        if (employeeDeptId > 0)
        {
            var mappedHod = await _context.DepartmentHodMappings
                .Where(m => m.DeptId == employeeDeptId)
                .Select(m => m.HodUserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (mappedHod > 0)
            {
                return mappedHod;
            }
        }

        var fallbackHod = await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.Name == "HOD")
            .Select(ur => ur.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (fallbackHod == 0)
        {
            throw new BusinessRuleException("No HOD found for this employee's department.");
        }

        return fallbackHod;
    }

    private async Task<int> ResolveHrUserAsync(CancellationToken cancellationToken)
    {
        var hrUserId = await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.Name == "HR")
            .Select(ur => ur.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (hrUserId == 0)
        {
            throw new BusinessRuleException("No HR user found in the system.");
        }

        return hrUserId;
    }

    private async Task CreateOrUpdateLowPerformerPipCaseAsync(Evaluation evaluation, string? comment, CancellationToken cancellationToken)
    {
        var assignedHrUserId = await ResolveHrUserAsync(cancellationToken);
        var pipCase = await _context.PipCases
            .FirstOrDefaultAsync(c => c.EvaluationId == evaluation.EvaluationId, cancellationToken);

        if (pipCase == null)
        {
            pipCase = new PipCase
            {
                EvaluationId = evaluation.EvaluationId,
                EmployeeUserId = evaluation.EmployeeId,
                AssignedHrUserId = assignedHrUserId,
                Status = "Open",
                Reason = comment ?? "Score below threshold (80).",
                CreatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddMonths(1)
            };
            _context.PipCases.Add(pipCase);
        }
        else
        {
            pipCase.AssignedHrUserId = assignedHrUserId;
            pipCase.Status = "Open";
            pipCase.Reason = comment ?? pipCase.Reason;
            pipCase.ClosedAt = null;
        }

        _context.Notifications.Add(new Notification
        {
            UserId = assignedHrUserId,
            Subject = "Low performer PIP case requires action",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        });
    }

    private async Task<decimal> CalculateFinalWeightedScoreAsync(int evaluationId, CancellationToken cancellationToken)
    {
        await EnsureDefaultWeightsAsync(cancellationToken);

        var weights = await _context.WorkflowReviewWeights
            .ToDictionaryAsync(w => w.ReviewerKey, w => w.WeightPercent, cancellationToken);

        var reviews = await _context.Reviews
            .Where(r => r.EvaluationId == evaluationId)
            .ToListAsync(cancellationToken);

        decimal self = NormalizeScore(reviews.FirstOrDefault(r => r.ReviewerRole == ReviewerRole.Self)?.OverallScore);
        decimal tl = NormalizeScore(reviews
            .Where(r => r.ReviewerRole == ReviewerRole.TL)
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => r.OverallScore)
            .FirstOrDefault());
        decimal rm = NormalizeScore(reviews
            .Where(r => r.ReviewerRole == ReviewerRole.RM)
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => r.OverallScore)
            .FirstOrDefault());

        var peerScores = reviews
            .Where(r => r.ReviewerRole == ReviewerRole.Peer)
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => NormalizeScore(r.OverallScore))
            .Take(2)
            .ToList();

        var peer1 = peerScores.Count > 0 ? peerScores[0] : 0m;
        var peer2 = peerScores.Count > 1 ? peerScores[1] : 0m;

        var total = (
            self * weights.GetValueOrDefault("Self") +
            tl * weights.GetValueOrDefault("TL") +
            rm * weights.GetValueOrDefault("RM") +
            peer1 * weights.GetValueOrDefault("Peer1") +
            peer2 * weights.GetValueOrDefault("Peer2")) / 100m;

        return Math.Round(total, 2);
    }

    private static decimal NormalizeScore(decimal? rawScore)
    {
        if (!rawScore.HasValue)
        {
            return 0m;
        }

        var value = rawScore.Value;
        return value <= 10m ? value * 10m : value;
    }

    private async Task EnsureDefaultWeightsAsync(CancellationToken cancellationToken)
    {
        var hasAny = await _context.WorkflowReviewWeights.AnyAsync(cancellationToken);
        if (hasAny)
        {
            return;
        }

        _context.WorkflowReviewWeights.AddRange(
            new WorkflowReviewWeight { ReviewerKey = "Self", WeightPercent = 20m, CreatedAt = DateTime.UtcNow },
            new WorkflowReviewWeight { ReviewerKey = "TL", WeightPercent = 20m, CreatedAt = DateTime.UtcNow },
            new WorkflowReviewWeight { ReviewerKey = "RM", WeightPercent = 20m, CreatedAt = DateTime.UtcNow },
            new WorkflowReviewWeight { ReviewerKey = "Peer1", WeightPercent = 20m, CreatedAt = DateTime.UtcNow },
            new WorkflowReviewWeight { ReviewerKey = "Peer2", WeightPercent = 20m, CreatedAt = DateTime.UtcNow }
        );

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static PipCaseDto MapPipCase(PipCase pipCase)
    {
        return new PipCaseDto
        {
            PipCaseId = pipCase.PipCaseId,
            EvaluationId = pipCase.EvaluationId,
            EmployeeUserId = pipCase.EmployeeUserId,
            EmployeeName = pipCase.EmployeeUser?.FullName ?? string.Empty,
            AssignedHrUserId = pipCase.AssignedHrUserId,
            AssignedHrName = pipCase.AssignedHrUser?.FullName ?? string.Empty,
            Status = pipCase.Status,
            Reason = pipCase.Reason,
            CreatedAt = pipCase.CreatedAt,
            DueDate = pipCase.DueDate,
            ClosedAt = pipCase.ClosedAt,
            ActionItems = pipCase.ActionItems
                .OrderBy(a => a.CreatedAt)
                .Select(a => new PipActionItemDto
                {
                    PipActionItemId = a.PipActionItemId,
                    Title = a.Title,
                    Description = a.Description,
                    TrainingMaterialId = a.TrainingMaterialId,
                    ExternalTrainingLink = a.ExternalTrainingLink,
                    DueDate = a.DueDate,
                    Status = a.Status,
                    CreatedAt = a.CreatedAt,
                    CompletedAt = a.CompletedAt
                })
                .ToList()
        };
    }
}

