using Epecps.Application.DTOs.EmployeeGoals;

namespace Epecps.Application.Interfaces;

/// <summary>
/// Service interface for browsing the goal framework (read-only for employees)
/// </summary>
public interface IGoalFrameworkService
{
    /// <summary>
    /// Get all active categories available for goal-setting
    /// </summary>
    Task<List<GoalFrameworkCategoryDto>> GetCategoriesAsync();

    /// <summary>
    /// Get all active items for a given category
    /// </summary>
    Task<List<GoalFrameworkItemDto>> GetItemsByCategoryAsync(Guid categoryId);

    /// <summary>
    /// Get all active goal items for a given item, including suggested activities
    /// </summary>
    Task<List<GoalFrameworkGoalItemDto>> GetGoalItemsByItemAsync(Guid itemId);
}
