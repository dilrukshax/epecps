using Epecps.Application.DTOs.EmployeeGoals;
using Epecps.Application.Interfaces;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// Service implementation for browsing the goal framework (read-only for employees)
/// </summary>
public class GoalFrameworkService : IGoalFrameworkService
{
    private readonly EpecpsDbContext _context;

    public GoalFrameworkService(EpecpsDbContext context)
    {
        _context = context;
    }

    public async Task<List<GoalFrameworkCategoryDto>> GetCategoriesAsync()
    {
        // Get only active categories from published templates that have active items
        var categories = await _context.ScoreCategories
            .Where(c => c.IsActive && c.Template.IsPublished && !c.Template.IsArchived)
            .Where(c => c.Items.Any(i => i.IsActive))
            .Select(c => new GoalFrameworkCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ItemCount = c.Items.Count(i => i.IsActive)
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories;
    }

    public async Task<List<GoalFrameworkItemDto>> GetItemsByCategoryAsync(Guid categoryId)
    {
        // Return ScoreItems as "Items"
        var items = await _context.ScoreItems
            .Where(i => i.ScoreCategoryId == categoryId && i.IsActive && i.Category.IsActive)
            .OrderBy(i => i.DisplayOrder)
            .Select(i => new GoalFrameworkItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                GoalItemCount = 1 // Each item has itself as the goal item
            })
            .ToListAsync();

        return items;
    }

    public async Task<List<GoalFrameworkGoalItemDto>> GetGoalItemsByItemAsync(Guid itemId)
    {
        // Return the ScoreItem as a GoalItem WITHOUT suggested activities
        var goalItems = await _context.ScoreItems
            .Where(i => i.Id == itemId && i.IsActive)
            .Select(i => new GoalFrameworkGoalItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                TargetScore = i.TargetScore,
                SuggestedActivities = new List<SuggestedActivityDto>() // Always empty - no suggested activities
            })
            .ToListAsync();

        return goalItems;
    }
}
