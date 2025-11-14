using Epecps.Application.DTOs.ScoreTemplates;

namespace Epecps.Application.Interfaces;

/// <summary>
/// Service for managing score items within categories
/// </summary>
public interface IScoreItemService
{
    /// <summary>
    /// Create a new item within a category
    /// </summary>
    Task<Guid> CreateItemAsync(Guid categoryId, CreateScoreItemDto dto, int userId);

    /// <summary>
    /// Update an existing item
    /// </summary>
    Task UpdateItemAsync(Guid itemId, UpdateScoreItemDto dto, int userId);

    /// <summary>
    /// Delete an item (soft delete if template is published)
    /// </summary>
    Task DeleteItemAsync(Guid itemId, int userId);
}
