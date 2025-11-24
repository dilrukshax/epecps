using Epecps.Application.DTOs.EmployeeGoals;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Domain.Enums;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// Service implementation for managing employee personal goals
/// </summary>
public class PersonalGoalService : IPersonalGoalService
{
    private readonly EpecpsDbContext _context;

    public PersonalGoalService(EpecpsDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> CreatePersonalGoalAsync(int userId, CreatePersonalGoalDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Load the goal item (ScoreItem) to get the target score and validate
            var goalItem = await _context.ScoreItems
                .FirstOrDefaultAsync(gi => gi.Id == dto.GoalItemId && gi.IsActive, cancellationToken);

            if (goalItem == null)
                throw new NotFoundException(nameof(ScoreItem), dto.GoalItemId);

            // Create the personal goal
            var personalGoal = new PersonalGoal
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GoalItemId = dto.GoalItemId,
                GoalSetId = dto.GoalSetId, // Set the goal set ID for grouping
                Title = dto.Title,
                Description = dto.Description,
                TargetScore = goalItem.TargetScore, // Default from framework (typically 100)
                StartDate = dto.StartDate,
                DueDate = dto.DueDate,
                Status = PersonalGoalStatus.InProgress, // Start as InProgress
                CurrentScore = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.PersonalGoals.Add(personalGoal);

            // Create custom activities ONLY
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
                ItemName = pg.GoalItem.Name, // Using ScoreItem name as Item name
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

        // Group goals by GoalSetId
        var groupedGoals = allGoals
            .GroupBy(g => g.GoalSetId ?? Guid.Empty)
            .Select(group =>
            {
                var totalTargetScore = group.Sum(g => g.TargetScore);
                var totalCurrentScore = group.Sum(g => g.CurrentScore);
                var progressPercent = totalTargetScore > 0 
                    ? Math.Round((totalCurrentScore / totalTargetScore) * 100, 2) 
                    : 0;
                
                // Can submit for evaluation if all goals are at 100% and status is Completed
                var canSubmit = progressPercent >= 100 && 
                               group.All(g => g.Status == PersonalGoalStatus.Completed);

                return new PersonalGoalSetDto
                {
                    GoalSetId = group.Key == Guid.Empty ? group.First().Id : group.Key,
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
                    }).ToList()
                };
            })
            .OrderByDescending(s => s.CreatedAt)
            .ToList();

        return groupedGoals;
    }

    private PersonalGoalStatus DetermineOverallStatus(List<PersonalGoal> goals)
    {
        if (goals.All(g => g.Status == PersonalGoalStatus.Completed))
            return PersonalGoalStatus.Completed;
        
        if (goals.Any(g => g.Status == PersonalGoalStatus.Cancelled))
            return PersonalGoalStatus.Cancelled;
        
        if (goals.Any(g => g.Status == PersonalGoalStatus.InProgress))
            return PersonalGoalStatus.InProgress;
        
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

        // Basic business rule: if goal is completed or cancelled, limit updates
        if (goal.Status == PersonalGoalStatus.Completed || goal.Status == PersonalGoalStatus.Cancelled)
        {
            // Allow only status changes for completed/cancelled goals
            if (dto.Status != goal.Status)
            {
                goal.Status = dto.Status;
                goal.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }
            
            throw new BusinessRuleException("Cannot update a completed or cancelled goal except to change its status.");
        }

        // Update allowed fields
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

        // Validate score range
        if (dto.CurrentScore < 0 || dto.CurrentScore > goal.TargetScore)
            throw new BusinessRuleException($"Current score must be between 0 and {goal.TargetScore}.");

        goal.CurrentScore = dto.CurrentScore;
        goal.UpdatedAt = DateTime.UtcNow;

        // Auto-complete goal if score reaches target
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

        // Only allow custom activities
        if (string.IsNullOrWhiteSpace(dto.Description))
            throw new BusinessRuleException("Description is required for activities.");

        var activity = new PersonalGoalActivity
        {
            Id = Guid.NewGuid(),
            PersonalGoalId = goalId,
            SuggestedActivityId = null, // Always null - no suggested activities
            Description = dto.Description.Trim(),
            DueDate = dto.DueDate,
            IsFromTemplate = false, // Always false
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

        // Verify ownership
        if (activity.PersonalGoal.UserId != userId)
            throw new BusinessRuleException("You do not have permission to update this activity.");

        // Update fields
        activity.Description = dto.Description;
        activity.Status = dto.Status;
        activity.DueDate = dto.DueDate;
        activity.EvidenceUrl = dto.EvidenceUrl;
        activity.EvidenceNotes = dto.EvidenceNotes;
        activity.UpdatedAt = DateTime.UtcNow;

        activity.PersonalGoal.UpdatedAt = DateTime.UtcNow;

        // Auto-recalculate score based on completed activities
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

    /// <summary>
    /// Internal helper to recalculate goal score based on completed activities
    /// </summary>
    private Task RecalculateGoalScoreFromActivitiesInternalAsync(PersonalGoal goal)
    {
        var totalActivities = goal.Activities.Count;
        if (totalActivities == 0)
        {
            // No activities - keep current manual score
            return Task.CompletedTask;
        }

        var completedActivities = goal.Activities.Count(a => a.Status == ActivityStatus.Done);
        
        // Calculate score as percentage of completed activities
        var newScore = Math.Round((decimal)completedActivities / totalActivities * goal.TargetScore, 2);
        
        goal.CurrentScore = newScore;
        goal.UpdatedAt = DateTime.UtcNow;

        // Auto-complete goal if all activities are done
        if (completedActivities == totalActivities && goal.Status != PersonalGoalStatus.Completed)
        {
            goal.Status = PersonalGoalStatus.Completed;
        }
        else if (completedActivities < totalActivities && goal.Status == PersonalGoalStatus.Completed)
        {
            // Revert to InProgress if not all activities are done
            goal.Status = PersonalGoalStatus.InProgress;
        }

        return Task.CompletedTask;
    }

    public async Task SubmitGoalSetForEvaluationAsync(Guid goalSetId, int userId, CancellationToken cancellationToken = default)
    {
        // Get all goals in the goal set
        var goals = await _context.PersonalGoals
            .Where(pg => pg.GoalSetId == goalSetId && pg.UserId == userId)
            .ToListAsync(cancellationToken);

        if (!goals.Any())
            throw new NotFoundException("Goal set not found or you don't have permission to access it.");

        // Verify all goals are completed
        if (!goals.All(g => g.Status == PersonalGoalStatus.Completed))
            throw new BusinessRuleException("All goals in the set must be completed before submitting for evaluation.");

        // Verify all goals have reached 100%
        var allComplete = goals.All(g => g.TargetScore > 0 && g.CurrentScore >= g.TargetScore);
        if (!allComplete)
            throw new BusinessRuleException("All goals must have a score of 100% before submitting for evaluation.");

        // TODO: Implement actual evaluation workflow
        // For now, this is a placeholder that validates the submission is possible
        // In the future, this could:
        // - Create an evaluation request record
        // - Notify evaluators/supervisors
        // - Lock the goals from further editing
        // - Trigger approval workflow

        // Placeholder: Just log the submission intent
        // In production, you might update a status or create an evaluation record
    }
}
