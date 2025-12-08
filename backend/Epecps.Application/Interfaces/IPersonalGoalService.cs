using Epecps.Application.DTOs.EmployeeGoals;
using Epecps.Application.DTOs.Evaluations;

namespace Epecps.Application.Interfaces;

/// <summary>
/// Service interface for managing employee personal goals
/// </summary>
public interface IPersonalGoalService
{
    /// <summary>
    /// Create a new personal goal for the authenticated user
    /// </summary>
    Task<Guid> CreatePersonalGoalAsync(int userId, CreatePersonalGoalDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all personal goals for the authenticated user
    /// </summary>
    Task<List<PersonalGoalListDto>> GetMyGoalsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get personal goals grouped by goal set
    /// </summary>
    Task<List<PersonalGoalSetDto>> GetMyGoalSetsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed information about a specific personal goal
    /// </summary>
    Task<PersonalGoalDetailDto> GetGoalDetailsAsync(Guid goalId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a personal goal
    /// </summary>
    Task UpdatePersonalGoalAsync(Guid goalId, int userId, UpdatePersonalGoalDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the score/progress of a personal goal
    /// </summary>
    Task UpdateGoalScoreAsync(Guid goalId, int userId, UpdatePersonalGoalScoreDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new activity to a personal goal
    /// </summary>
    Task<Guid> AddActivityAsync(Guid goalId, int userId, CreatePersonalGoalActivityDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing activity
    /// </summary>
    Task UpdateActivityAsync(Guid goalId, Guid activityId, int userId, UpdatePersonalGoalActivityDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit a goal set for evaluation (starts the approval workflow)
    /// Creates an evaluation in PENDING_RM_REVIEW status
    /// </summary>
    Task<SubmitGoalSetResponseDto> SubmitGoalSetForEvaluationAsync(Guid goalSetId, int userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Start working on a goal after RM approval
    /// Goal must be in ApprovedByRM status
    /// </summary>
    /// <param name="goalId">The goal to start</param>
    /// <param name="userId">The authenticated user (must be goal owner)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response with updated goal status</returns>
    Task<GoalActionResponseDto> StartGoalAsync(Guid goalId, int userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mark a goal as completed
    /// Goal must be in InProgress status
    /// If all goals in the evaluation are completed, triggers the workflow to continue
    /// </summary>
    /// <param name="goalId">The goal to complete</param>
    /// <param name="userId">The authenticated user (must be goal owner)</param>
    /// <param name="dto">Optional completion details (evidence, comment, score)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response with updated goal status and workflow continuation info</returns>
    Task<GoalActionResponseDto> CompleteGoalAsync(Guid goalId, int userId, CompleteGoalRequestDto? dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Recalculate goal score based on completed activities
    /// </summary>
    Task RecalculateGoalScoreFromActivitiesAsync(Guid goalId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a personal goal (only if not submitted for evaluation)
    /// </summary>
    Task DeletePersonalGoalAsync(Guid goalId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an activity from a personal goal
    /// </summary>
    Task DeleteActivityAsync(Guid goalId, Guid activityId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an entire goal set (all goals in the set)
    /// </summary>
    Task DeleteGoalSetAsync(Guid goalSetId, int userId, CancellationToken cancellationToken = default);
}
