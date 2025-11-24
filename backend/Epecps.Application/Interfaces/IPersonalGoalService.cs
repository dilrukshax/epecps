using Epecps.Application.DTOs.EmployeeGoals;

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
}
