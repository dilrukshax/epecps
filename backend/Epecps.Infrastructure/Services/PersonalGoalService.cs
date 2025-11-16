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
                Title = dto.Title,
                Description = dto.Description,
                TargetScore = goalItem.TargetScore, // Default from framework
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
                Title = pg.Title,
                CategoryName = pg.GoalItem.Category.Name,
                ItemName = pg.GoalItem.Name, // Using ScoreItem name as Item name
                GoalItemName = pg.GoalItem.Name,
                TargetScore = pg.TargetScore,
                CurrentScore = pg.CurrentScore,
                Status = pg.Status,
                DueDate = pg.DueDate,
                CreatedAt = pg.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return goals;
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

        var dto = new PersonalGoalDetailDto
        {
            Id = goal.Id,
            UserId = goal.UserId,
            GoalItemId = goal.GoalItemId,
            Title = goal.Title,
            Description = goal.Description,
            TargetScore = goal.TargetScore,
            CurrentScore = goal.CurrentScore,
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

        await _context.SaveChangesAsync(cancellationToken);
    }
}
