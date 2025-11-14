using Epecps.Application.DTOs.ScoreTemplates;

namespace Epecps.Application.Interfaces;

/// <summary>
/// Service for managing score categories within templates
/// </summary>
public interface IScoreCategoryService
{
    /// <summary>
    /// Create a new category within a template
    /// </summary>
    Task<Guid> CreateCategoryAsync(Guid templateId, CreateScoreCategoryDto dto, int userId);

    /// <summary>
    /// Update an existing category
    /// </summary>
    Task UpdateCategoryAsync(Guid categoryId, UpdateScoreCategoryDto dto, int userId);

    /// <summary>
    /// Delete a category (soft delete if template is published)
    /// </summary>
    Task DeleteCategoryAsync(Guid categoryId, int userId);
}
