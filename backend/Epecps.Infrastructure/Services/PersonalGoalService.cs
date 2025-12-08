using Epecps.Application.DTOs.EmployeeGoals;
using Epecps.Application.DTOs.Evaluations;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Domain.Enums;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

public class PersonalGoalService : IPersonalGoalService
{
    private readonly EpecpsDbContext _context;
    private readonly IEvaluationWorkflowService _evaluationWorkflowService;

    // Evaluation status constants (must match EvaluationWorkflowService)
    private const string STATUS_APPROVED_BY_RM = "Approved_By_RM";

    public PersonalGoalService(EpecpsDbContext context, IEvaluationWorkflowService evaluationWorkflowService)
    {
        _context = context;
        _evaluationWorkflowService = evaluationWorkflowService;
    }

    public async Task<Guid> CreatePersonalGoalAsync(int userId, CreatePersonalGoalDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var goalItem = await _context.ScoreItems
                .FirstOrDefaultAsync(gi => gi.Id == dto.GoalItemId && gi.IsActive, cancellationToken);

            if (goalItem == null)
                throw new NotFoundException(nameof(ScoreItem), dto.GoalItemId);

            var personalGoal = new PersonalGoal
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GoalItemId = dto.GoalItemId,
                GoalSetId = dto.GoalSetId,
                Title = dto.Title,
                Description = dto.Description,
                TargetScore = goalItem.TargetScore,
                StartDate = dto.StartDate,
                DueDate = dto.DueDate,
                Status = PersonalGoalStatus.Draft, // Changed to Draft - goals start as draft until submitted
                CurrentScore = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.PersonalGoals.Add(personalGoal);

            foreach (var customActivityDesc in dto.CustomActivities)
            {
                if (!string.IsNullOrWhiteSpace(customActivityDesc))
                {
                    var activity = new PersonalGoalActivity
                    {
                        Id = Guid.NewGuid(),
                        PersonalGoalId = personalGoal.Id,
                        SuggestedActivityId = null,
                        Description = customActivityDesc.Trim(),
                        IsFromTemplate = false,
                        Status = ActivityStatus.NotStarted,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PersonalGoalActivities.Add(activity);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return personalGoal.Id;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception($"An error occurred while creating the personal goal: {ex.Message}", ex);
        }
    }

    public async Task<List<PersonalGoalListDto>> GetMyGoalsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var goals = await _context.PersonalGoals
            .Where(pg => pg.UserId == userId)
            .Include(pg => pg.GoalItem)
                .ThenInclude(gi => gi.Category)
            .OrderByDescending(pg => pg.CreatedAt)
            .Select(pg => new PersonalGoalListDto
            {
                Id = pg.Id,
                GoalSetId = pg.GoalSetId,
                Title = pg.Title,
                CategoryName = pg.GoalItem.Category.Name,
                ItemName = pg.GoalItem.Name,
                GoalItemName = pg.GoalItem.Name,
                TargetScore = pg.TargetScore,
                CurrentScore = pg.CurrentScore,
                ProgressPercent = pg.TargetScore > 0 ? Math.Round((pg.CurrentScore / pg.TargetScore) * 100, 2) : 0,
                Status = pg.Status,
                DueDate = pg.DueDate,
                CreatedAt = pg.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return goals;
    }

    public async Task<List<PersonalGoalSetDto>> GetMyGoalSetsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var allGoals = await _context.PersonalGoals
            .Where(pg => pg.UserId == userId)
            .Include(pg => pg.GoalItem)
                .ThenInclude(gi => gi.Category)
                    .ThenInclude(c => c.Template)
            .OrderByDescending(pg => pg.CreatedAt)
            .ToListAsync(cancellationToken);

        var groupedGoals = allGoals
            .GroupBy(g => g.GoalSetId ?? Guid.Empty)
            .Select(group =>
            {
                var totalTargetScore = group.Sum(g => g.TargetScore);
                var totalCurrentScore = group.Sum(g => g.CurrentScore);
                var progressPercent = totalTargetScore > 0 
                    ? Math.Round((totalCurrentScore / totalTargetScore) * 100, 2) 
                    : 0;
                
                var canSubmit = group.All(g => g.Status == PersonalGoalStatus.Draft || 
                                               g.Status == PersonalGoalStatus.ReturnedToEmployee);

                var goalSetId = group.Key == Guid.Empty ? group.First().Id : group.Key;

                return new PersonalGoalSetDto
                {
                    GoalSetId = goalSetId,
                    TemplateName = group.First().GoalItem.Category.Template.Name,
                    GoalCount = group.Count(),
                    TotalTargetScore = totalTargetScore,
                    TotalCurrentScore = totalCurrentScore,
                    ProgressPercent = progressPercent,
                    CanSubmitForEvaluation = canSubmit,
                    StartDate = group.First().StartDate,
                    DueDate = group.First().DueDate,
                    Status = DetermineOverallStatus(group.ToList()),
                    CreatedAt = group.First().CreatedAt,
                    Categories = group.Select(g => g.GoalItem.Category.Name).Distinct().OrderBy(c => c).ToList(),
                    Goals = group.Select(g => new PersonalGoalListDto
                    {
                        Id = g.Id,
                        GoalSetId = g.GoalSetId,
                        Title = g.Title,
                        CategoryName = g.GoalItem.Category.Name,
                        ItemName = g.GoalItem.Name,
                        GoalItemName = g.GoalItem.Name,
                        TargetScore = g.TargetScore,
                        CurrentScore = g.CurrentScore,
                        ProgressPercent = g.TargetScore > 0 ? Math.Round((g.CurrentScore / g.TargetScore) * 100, 2) : 0,
                        Status = g.Status,
                        DueDate = g.DueDate,
                        CreatedAt = g.CreatedAt
                    }).ToList(),
                    EvaluationInfo = GetEvaluationInfoForGoalSet(goalSetId, userId).Result
                };
            })
            .OrderByDescending(s => s.CreatedAt)
            .ToList();

        return groupedGoals;
    }

    private async Task<GoalSetEvaluationInfoDto?> GetEvaluationInfoForGoalSet(Guid goalSetId, int userId)
    {
        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.Reviews)
                .ThenInclude(r => r.Reviewer)
            .Include(e => e.PeerAssignments)
                .ThenInclude(pa => pa.PeerUser)
            .Include(e => e.ReportingManager)
            .Include(e => e.TeamLead)
            .Include(e => e.EmployeeGoals)
            .Where(e => e.GoalSetId == goalSetId && e.EmployeeId == userId)
            .OrderByDescending(e => e.EvaluationId)
            .FirstOrDefaultAsync();

        if (evaluation == null)
            return null;

        var approvalHistory = await _context.Set<ApprovalHistory>()
            .Include(ah => ah.ActorUser)
            .Where(ah => ah.EvaluationId == evaluation.EvaluationId)
            .OrderBy(ah => ah.CreatedAt)
            .ToListAsync();

        var approvalSteps = new List<GoalSetApprovalStepDto>();

        foreach (var history in approvalHistory)
        {
            var role = history.ActorRole;
            
            var existingStep = approvalSteps.FirstOrDefault(s => s.Role == role);
            if (existingStep == null)
            {
                approvalSteps.Add(new GoalSetApprovalStepDto
                {
                    Role = role,
                    ActorName = history.ActorUser.FullName,
                    Action = history.Action,
                    Comment = history.Comment,
                    ActionDate = history.CreatedAt,
                    IsCompleted = history.Action.Contains("Approved") || history.Action.Contains("Submitted") || history.Action.Contains("Recommended") || history.Action.Contains("Processed") || history.Action.Contains("Completed"),
                    IsPending = false,
                    IsRejected = history.Action.Contains("Rejected")
                });
            }
        }

        var status = evaluation.Status;
        if (status.Contains("Pending_RM"))
        {
            if (!approvalSteps.Any(s => s.Role == "RM"))
                approvalSteps.Add(new GoalSetApprovalStepDto { Role = "RM", Action = "Pending", IsPending = true, ActorName = evaluation.ReportingManager?.FullName ?? "RM" });
        }
        else if (status.Contains("Approved_By_RM"))
        {
            // Waiting for employee to start/complete goals
            approvalSteps.Add(new GoalSetApprovalStepDto { Role = "Employee", Action = "ReadyToStart", IsPending = true, ActorName = "You" });
        }
        else if (status.Contains("Pending_TL"))
        {
            if (!approvalSteps.Any(s => s.Role == "TL"))
                approvalSteps.Add(new GoalSetApprovalStepDto { Role = "TL", Action = "Pending", IsPending = true, ActorName = evaluation.TeamLead?.FullName ?? "TL" });
        }
        else if (status.Contains("Pending_Peer"))
        {
            var peers = evaluation.PeerAssignments.ToList();
            foreach (var peer in peers)
            {
                var peerReview = evaluation.Reviews.FirstOrDefault(r => r.ReviewerUserId == peer.PeerUserId && r.ReviewerRole == ReviewerRole.Peer);
                if (peerReview != null && peerReview.Status == "Pending")
                {
                    approvalSteps.Add(new GoalSetApprovalStepDto 
                    { 
                        Role = "Peer", 
                        Action = "Pending", 
                        IsPending = true, 
                        ActorName = peer.PeerUser?.FullName ?? "Peer" 
                    });
                }
            }
        }
        else if (status.Contains("Pending_HOD"))
        {
            approvalSteps.Add(new GoalSetApprovalStepDto { Role = "HOD", Action = "Pending", IsPending = true, ActorName = "HOD" });
        }

        return new GoalSetEvaluationInfoDto
        {
            EvaluationId = evaluation.EvaluationId,
            Status = evaluation.Status,
            OverallScore = evaluation.OverallScore,
            SubmittedDate = approvalHistory.FirstOrDefault(ah => ah.Action == "SubmittedToRM")?.CreatedAt ?? DateTime.UtcNow,
            CompletedDate = status.Contains("Completed") ? approvalHistory.LastOrDefault()?.CreatedAt : null,
            ApprovalSteps = approvalSteps.OrderBy(s => GetStepOrder(s.Role)).ToList()
        };
    }

    private int GetStepOrder(string role)
    {
        return role switch
        {
            "Employee" => 1,
            "RM" => 2,
            "TL" => 3,
            "Peer" => 4,
            "HOD" => 5,
            "GM" => 6,
            _ => 99
        };
    }

    private PersonalGoalStatus DetermineOverallStatus(List<PersonalGoal> goals)
    {
        if (goals.All(g => g.Status == PersonalGoalStatus.Completed))
            return PersonalGoalStatus.Completed;
        
        if (goals.Any(g => g.Status == PersonalGoalStatus.Cancelled))
            return PersonalGoalStatus.Cancelled;
        
        if (goals.Any(g => g.Status == PersonalGoalStatus.InProgress))
            return PersonalGoalStatus.InProgress;
        
        if (goals.Any(g => g.Status == PersonalGoalStatus.ApprovedByRM))
            return PersonalGoalStatus.ApprovedByRM;
        
        if (goals.Any(g => g.Status == PersonalGoalStatus.PendingRMReview))
            return PersonalGoalStatus.PendingRMReview;
        
        if (goals.Any(g => g.Status == PersonalGoalStatus.ReturnedToEmployee))
            return PersonalGoalStatus.ReturnedToEmployee;
        
        return PersonalGoalStatus.Draft;
    }

    public async Task<PersonalGoalDetailDto> GetGoalDetailsAsync(Guid goalId, int userId, CancellationToken cancellationToken = default)
    {
        var goal = await _context.PersonalGoals
            .Where(pg => pg.Id == goalId && pg.UserId == userId)
            .Include(pg => pg.GoalItem)
                .ThenInclude(gi => gi.Category)
            .Include(pg => pg.Activities)
            .FirstOrDefaultAsync(cancellationToken);

        if (goal == null)
            throw new NotFoundException(nameof(PersonalGoal), goalId);

        var progressPercent = goal.TargetScore > 0 
            ? Math.Round((goal.CurrentScore / goal.TargetScore) * 100, 2) 
            : 0;

        var dto = new PersonalGoalDetailDto
        {
            Id = goal.Id,
            UserId = goal.UserId,
            GoalItemId = goal.GoalItemId,
            Title = goal.Title,
            Description = goal.Description,
            TargetScore = goal.TargetScore,
            CurrentScore = goal.CurrentScore,
            ProgressPercent = progressPercent,
            StartDate = goal.StartDate,
            DueDate = goal.DueDate,
            Status = goal.Status,
            CreatedAt = goal.CreatedAt,
            UpdatedAt = goal.UpdatedAt,
            CategoryName = goal.GoalItem.Category.Name,
            ItemName = goal.GoalItem.Name,
            GoalItemName = goal.GoalItem.Name,
            GoalItemDescription = goal.GoalItem.Description,
            Activities = goal.Activities
                .OrderBy(a => a.CreatedAt)
                .Select(a => new PersonalGoalActivityDto
                {
                    Id = a.Id,
                    PersonalGoalId = a.PersonalGoalId,
                    SuggestedActivityId = a.SuggestedActivityId,
                    Description = a.Description,
                    IsFromTemplate = a.IsFromTemplate,
                    Status = a.Status,
                    DueDate = a.DueDate,
                    EvidenceUrl = a.EvidenceUrl,
                    EvidenceNotes = a.EvidenceNotes,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .ToList()
        };

        return dto;
    }

    public async Task UpdatePersonalGoalAsync(Guid goalId, int userId, UpdatePersonalGoalDto dto, CancellationToken cancellationToken = default)
    {
        var goal = await _context.PersonalGoals
            .FirstOrDefaultAsync(pg => pg.Id == goalId && pg.UserId == userId, cancellationToken);

        if (goal == null)
            throw new NotFoundException(nameof(PersonalGoal), goalId);

        if (goal.Status == PersonalGoalStatus.Completed || goal.Status == PersonalGoalStatus.Cancelled)
        {
            if (dto.Status != goal.Status)
            {
                goal.Status = dto.Status;
                goal.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }
            
            throw new BusinessRuleException("Cannot update a completed or cancelled goal except to change its status.");
        }

        goal.Title = dto.Title;
        goal.Description = dto.Description;
        goal.StartDate = dto.StartDate;
        goal.DueDate = dto.DueDate;
        goal.Status = dto.Status;
        goal.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateGoalScoreAsync(Guid goalId, int userId, UpdatePersonalGoalScoreDto dto, CancellationToken cancellationToken = default)
    {
        var goal = await _context.PersonalGoals
            .FirstOrDefaultAsync(pg => pg.Id == goalId && pg.UserId == userId, cancellationToken);

        if (goal == null)
            throw new NotFoundException(nameof(PersonalGoal), goalId);

        if (dto.CurrentScore < 0 || dto.CurrentScore > goal.TargetScore)
            throw new BusinessRuleException($"Current score must be between 0 and {goal.TargetScore}.");

        goal.CurrentScore = dto.CurrentScore;
        goal.UpdatedAt = DateTime.UtcNow;

        if (dto.CurrentScore >= goal.TargetScore && goal.Status != PersonalGoalStatus.Completed)
        {
            goal.Status = PersonalGoalStatus.Completed;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> AddActivityAsync(Guid goalId, int userId, CreatePersonalGoalActivityDto dto, CancellationToken cancellationToken = default)
    {
        var goal = await _context.PersonalGoals
            .Include(pg => pg.Activities)
            .FirstOrDefaultAsync(pg => pg.Id == goalId && pg.UserId == userId, cancellationToken);

        if (goal == null)
            throw new NotFoundException(nameof(PersonalGoal), goalId);

        if (string.IsNullOrWhiteSpace(dto.Description))
            throw new BusinessRuleException("Description is required for activities.");

        var activity = new PersonalGoalActivity
        {
            Id = Guid.NewGuid(),
            PersonalGoalId = goalId,
            SuggestedActivityId = null,
            Description = dto.Description.Trim(),
            DueDate = dto.DueDate,
            IsFromTemplate = false,
            Status = ActivityStatus.NotStarted,
            CreatedAt = DateTime.UtcNow
        };

        _context.PersonalGoalActivities.Add(activity);
        
        goal.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);

        return activity.Id;
    }

    public async Task UpdateActivityAsync(Guid goalId, Guid activityId, int userId, UpdatePersonalGoalActivityDto dto, CancellationToken cancellationToken = default)
    {
        var activity = await _context.PersonalGoalActivities
            .Include(a => a.PersonalGoal)
                .ThenInclude(pg => pg.Activities)
            .FirstOrDefaultAsync(a => a.Id == activityId && a.PersonalGoalId == goalId, cancellationToken);

        if (activity == null)
            throw new NotFoundException(nameof(PersonalGoalActivity), activityId);

        if (activity.PersonalGoal.UserId != userId)
            throw new BusinessRuleException("You do not have permission to update this activity.");

        activity.Description = dto.Description;
        activity.Status = dto.Status;
        activity.DueDate = dto.DueDate;
        activity.EvidenceUrl = dto.EvidenceUrl;
        activity.EvidenceNotes = dto.EvidenceNotes;
        activity.UpdatedAt = DateTime.UtcNow;

        activity.PersonalGoal.UpdatedAt = DateTime.UtcNow;

        await RecalculateGoalScoreFromActivitiesInternalAsync(activity.PersonalGoal);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RecalculateGoalScoreFromActivitiesAsync(Guid goalId, int userId, CancellationToken cancellationToken = default)
    {
        var goal = await _context.PersonalGoals
            .Include(pg => pg.Activities)
            .FirstOrDefaultAsync(pg => pg.Id == goalId && pg.UserId == userId, cancellationToken);

        if (goal == null)
            throw new NotFoundException(nameof(PersonalGoal), goalId);

        await RecalculateGoalScoreFromActivitiesInternalAsync(goal);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private Task RecalculateGoalScoreFromActivitiesInternalAsync(PersonalGoal goal)
    {
        var totalActivities = goal.Activities.Count;
        if (totalActivities == 0)
        {
            return Task.CompletedTask;
        }

        var completedActivities = goal.Activities.Count(a => a.Status == ActivityStatus.Done);
        
        var newScore = Math.Round((decimal)completedActivities / totalActivities * goal.TargetScore, 2);
        
        goal.CurrentScore = newScore;
        goal.UpdatedAt = DateTime.UtcNow;

        if (completedActivities == totalActivities && goal.Status == PersonalGoalStatus.InProgress)
        {
            goal.Status = PersonalGoalStatus.Completed;
            goal.CompletedAt = DateTime.UtcNow;
        }
        else if (completedActivities < totalActivities && goal.Status == PersonalGoalStatus.Completed)
        {
            goal.Status = PersonalGoalStatus.InProgress;
            goal.CompletedAt = null;
        }

        return Task.CompletedTask;
    }

    public async Task<SubmitGoalSetResponseDto> SubmitGoalSetForEvaluationAsync(Guid goalSetId, int userId, CancellationToken cancellationToken = default)
    {
        var goals = await _context.PersonalGoals
            .Where(pg => pg.GoalSetId == goalSetId && pg.UserId == userId)
            .ToListAsync(cancellationToken);

        if (!goals.Any())
            throw new NotFoundException("Goal set not found or you don't have permission to access it.");

        // Check if goals can be submitted (must be in Draft or ReturnedToEmployee status)
        var invalidGoals = goals.Where(g => g.Status != PersonalGoalStatus.Draft && 
                                            g.Status != PersonalGoalStatus.ReturnedToEmployee);
        if (invalidGoals.Any())
        {
            var statuses = string.Join(", ", invalidGoals.Select(g => g.Status.ToString()).Distinct());
            throw new BusinessRuleException($"Goals must be in Draft or ReturnedToEmployee status to submit. Found goals in: {statuses}");
        }

        // Check if there's already an active evaluation for this goal set
        var existingEvaluation = await _context.Set<Evaluation>()
            .Where(e => e.GoalSetId == goalSetId && e.EmployeeId == userId)
            .Where(e => !e.Status.Contains("Completed") && e.Status != "Rejected" && e.Status != "Returned_To_Employee")
            .FirstOrDefaultAsync(cancellationToken);

        if (existingEvaluation != null)
            throw new BusinessRuleException($"An active evaluation already exists for this goal set (Status: {existingEvaluation.Status}).");

        var activeCycle = await _context.Set<Cycle>()
            .Where(c => c.StartDate <= DateTime.UtcNow && c.EndDate >= DateTime.UtcNow)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeCycle == null)
        {
            activeCycle = new Cycle
            {
                Name = $"Cycle {DateTime.UtcNow.Year}",
                StartDate = new DateTime(DateTime.UtcNow.Year, 1, 1),
                EndDate = new DateTime(DateTime.UtcNow.Year, 12, 31),
                Status = "Active"
            };
            _context.Set<Cycle>().Add(activeCycle);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var evaluation = await _evaluationWorkflowService.StartEvaluationForGoalSetAsync(
            userId, 
            goalSetId, 
            activeCycle.CycleId, 
            cancellationToken);

        return new SubmitGoalSetResponseDto
        {
            EvaluationId = evaluation.EvaluationId,
            Status = evaluation.Status,
            Message = "Goal set submitted for RM review. Your Reporting Manager will be notified."
        };
    }

    /// <summary>
    /// Start working on a goal after RM approval
    /// </summary>
    public async Task<GoalActionResponseDto> StartGoalAsync(Guid goalId, int userId, CancellationToken cancellationToken = default)
    {
        var goal = await _context.PersonalGoals
            .FirstOrDefaultAsync(pg => pg.Id == goalId && pg.UserId == userId, cancellationToken);

        if (goal == null)
            throw new NotFoundException(nameof(PersonalGoal), goalId);

        // Validate goal is in correct status to be started
        if (goal.Status != PersonalGoalStatus.ApprovedByRM)
            throw new BusinessRuleException($"Goal can only be started when approved by RM. Current status: {goal.Status}");

        // Find the related evaluation
        Evaluation? evaluation = null;
        if (goal.GoalSetId.HasValue)
        {
            evaluation = await _context.Set<Evaluation>()
                .Where(e => e.GoalSetId == goal.GoalSetId && e.EmployeeId == userId)
                .Where(e => e.Status == STATUS_APPROVED_BY_RM)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var oldStatus = goal.Status;

        // Update goal status
        goal.Status = PersonalGoalStatus.InProgress;
        goal.StartedAt = DateTime.UtcNow;
        goal.UpdatedAt = DateTime.UtcNow;

        // Create approval history if we have an evaluation
        if (evaluation != null)
        {
            var approvalHistory = new ApprovalHistory
            {
                EvaluationId = evaluation.EvaluationId,
                ReviewId = null,
                ActorUserId = userId,
                ActorRole = "Employee",
                Action = "EmployeeStartedGoal",
                Comment = $"Started goal: {goal.Title}",
                FromStatus = oldStatus.ToString(),
                ToStatus = goal.Status.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<ApprovalHistory>().Add(approvalHistory);

            // Create audit log
            var auditLog = new AuditLog
            {
                ActorUserId = userId,
                EntityType = "PersonalGoal",
                EntityId = 0, // PersonalGoal uses Guid, but AuditLog uses int
                Action = "GOAL_STARTED",
                BeforeJson = System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus.ToString(), GoalId = goalId }),
                AfterJson = System.Text.Json.JsonSerializer.Serialize(new { Status = goal.Status.ToString(), StartedAt = goal.StartedAt }),
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<AuditLog>().Add(auditLog);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new GoalActionResponseDto
        {
            GoalId = goalId,
            Status = goal.Status.ToString(),
            Message = "Goal started successfully. Good luck!",
            WorkflowContinued = false,
            EvaluationId = evaluation?.EvaluationId,
            EvaluationStatus = evaluation?.Status
        };
    }

    /// <summary>
    /// Mark a goal as completed and optionally continue workflow if all goals are done
    /// </summary>
    public async Task<GoalActionResponseDto> CompleteGoalAsync(Guid goalId, int userId, CompleteGoalRequestDto? dto, CancellationToken cancellationToken = default)
    {
        var goal = await _context.PersonalGoals
            .FirstOrDefaultAsync(pg => pg.Id == goalId && pg.UserId == userId, cancellationToken);

        if (goal == null)
            throw new NotFoundException(nameof(PersonalGoal), goalId);

        // Validate goal is in progress
        if (goal.Status != PersonalGoalStatus.InProgress)
            throw new BusinessRuleException($"Goal can only be completed when in progress. Current status: {goal.Status}");

        // Validate score if provided
        var newScore = dto?.CurrentScore ?? goal.TargetScore;
        if (newScore < 0 || newScore > goal.TargetScore)
            throw new BusinessRuleException($"Score must be between 0 and {goal.TargetScore}.");

        var oldStatus = goal.Status;

        // Update goal
        goal.Status = PersonalGoalStatus.Completed;
        goal.CurrentScore = newScore;
        goal.CompletedAt = DateTime.UtcNow;
        goal.UpdatedAt = DateTime.UtcNow;

        // Find the related evaluation
        Evaluation? evaluation = null;
        bool workflowContinued = false;

        if (goal.GoalSetId.HasValue)
        {
            evaluation = await _context.Set<Evaluation>()
                .Where(e => e.GoalSetId == goal.GoalSetId && e.EmployeeId == userId)
                .Where(e => e.Status == STATUS_APPROVED_BY_RM)
                .FirstOrDefaultAsync(cancellationToken);

            if (evaluation != null)
            {
                // Create approval history for goal completion
                var approvalHistory = new ApprovalHistory
                {
                    EvaluationId = evaluation.EvaluationId,
                    ReviewId = null,
                    ActorUserId = userId,
                    ActorRole = "Employee",
                    Action = "EmployeeCompletedGoal",
                    Comment = dto?.Comment ?? $"Completed goal: {goal.Title}",
                    FromStatus = oldStatus.ToString(),
                    ToStatus = goal.Status.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Set<ApprovalHistory>().Add(approvalHistory);

                // Create audit log
                var auditLog = new AuditLog
                {
                    ActorUserId = userId,
                    EntityType = "PersonalGoal",
                    EntityId = 0,
                    Action = "GOAL_COMPLETED",
                    BeforeJson = System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus.ToString(), GoalId = goalId }),
                    AfterJson = System.Text.Json.JsonSerializer.Serialize(new { Status = goal.Status.ToString(), CompletedAt = goal.CompletedAt, Score = goal.CurrentScore }),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Set<AuditLog>().Add(auditLog);

                await _context.SaveChangesAsync(cancellationToken);

                // Check if all goals in the goal set are completed
                var allGoalsInSet = await _context.PersonalGoals
                    .Where(pg => pg.GoalSetId == goal.GoalSetId && pg.UserId == userId)
                    .ToListAsync(cancellationToken);

                var allCompleted = allGoalsInSet.All(g => g.Status == PersonalGoalStatus.Completed);

                if (allCompleted)
                {
                    // Update all goals to UnderEvaluation
                    foreach (var g in allGoalsInSet)
                    {
                        g.Status = PersonalGoalStatus.UnderEvaluation;
                        g.UpdatedAt = DateTime.UtcNow;
                    }

                    // Continue the workflow to TL review
                    evaluation = await _evaluationWorkflowService.ContinueWorkflowAfterEmployeeCompletionAsync(
                        evaluation.EvaluationId, 
                        cancellationToken);

                    workflowContinued = true;
                }
            }
            else
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new GoalActionResponseDto
        {
            GoalId = goalId,
            Status = goal.Status.ToString(),
            Message = workflowContinued 
                ? "All goals completed! Your evaluation has been forwarded to the Team Lead for review."
                : "Goal completed successfully.",
            WorkflowContinued = workflowContinued,
            EvaluationId = evaluation?.EvaluationId,
            EvaluationStatus = evaluation?.Status
        };
    }

    public async Task DeletePersonalGoalAsync(Guid goalId, int userId, CancellationToken cancellationToken = default)
    {
        var goal = await _context.PersonalGoals
            .Include(pg => pg.Activities)
            .FirstOrDefaultAsync(pg => pg.Id == goalId && pg.UserId == userId, cancellationToken);

        if (goal == null)
            throw new NotFoundException(nameof(PersonalGoal), goalId);

        // Check if goal has been submitted for evaluation
        if (goal.GoalSetId.HasValue)
        {
            var evaluation = await _context.Set<Evaluation>()
                .FirstOrDefaultAsync(e => e.GoalSetId == goal.GoalSetId && e.EmployeeId == userId, cancellationToken);

            if (evaluation != null && evaluation.Status != "Returned_To_Employee")
            {
                throw new BusinessRuleException("Cannot delete a goal that has been submitted for evaluation. Please contact your supervisor if you need to make changes.");
            }
        }

        _context.PersonalGoalActivities.RemoveRange(goal.Activities);
        _context.PersonalGoals.Remove(goal);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteActivityAsync(Guid goalId, Guid activityId, int userId, CancellationToken cancellationToken = default)
    {
        var activity = await _context.PersonalGoalActivities
            .Include(a => a.PersonalGoal)
                .ThenInclude(pg => pg.Activities)
            .FirstOrDefaultAsync(a => a.Id == activityId && a.PersonalGoalId == goalId, cancellationToken);

        if (activity == null)
            throw new NotFoundException(nameof(PersonalGoalActivity), activityId);

        if (activity.PersonalGoal.UserId != userId)
            throw new BusinessRuleException("You do not have permission to delete this activity.");

        if (activity.PersonalGoal.GoalSetId.HasValue)
        {
            var evaluation = await _context.Set<Evaluation>()
                .FirstOrDefaultAsync(e => e.GoalSetId == activity.PersonalGoal.GoalSetId && e.EmployeeId == userId, cancellationToken);

            if (evaluation != null && evaluation.Status != "Returned_To_Employee")
            {
                throw new BusinessRuleException("Cannot delete an activity from a goal that has been submitted for evaluation.");
            }
        }

        _context.PersonalGoalActivities.Remove(activity);

        await RecalculateGoalScoreFromActivitiesInternalAsync(activity.PersonalGoal);

        activity.PersonalGoal.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteGoalSetAsync(Guid goalSetId, int userId, CancellationToken cancellationToken = default)
    {
        var goals = await _context.PersonalGoals
            .Include(pg => pg.Activities)
            .Where(pg => pg.GoalSetId == goalSetId && pg.UserId == userId)
            .ToListAsync(cancellationToken);

        if (!goals.Any())
            throw new NotFoundException("Goal set not found or you don't have permission to access it.");

        var evaluation = await _context.Set<Evaluation>()
            .FirstOrDefaultAsync(e => e.GoalSetId == goalSetId && e.EmployeeId == userId, cancellationToken);

        if (evaluation != null && evaluation.Status != "Returned_To_Employee")
        {
            throw new BusinessRuleException("Cannot delete a goal set that has been submitted for evaluation. Please contact your supervisor if you need to make changes.");
        }

        foreach (var goal in goals)
        {
            _context.PersonalGoalActivities.RemoveRange(goal.Activities);
        }

        _context.PersonalGoals.RemoveRange(goals);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
