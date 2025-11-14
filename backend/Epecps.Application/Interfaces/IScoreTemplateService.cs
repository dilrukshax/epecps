using Epecps.Application.DTOs.ScoreTemplates;

namespace Epecps.Application.Interfaces;

/// <summary>
/// Service for managing score templates
/// </summary>
public interface IScoreTemplateService
{
    /// <summary>
    /// Get all score templates
    /// </summary>
    Task<List<ScoreTemplateListDto>> GetAllAsync(bool includeArchived = false);

    /// <summary>
    /// Get a score template by ID with all its categories and items
    /// </summary>
    Task<ScoreTemplateDetailDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Create a new score template
    /// </summary>
    Task<Guid> CreateTemplateAsync(CreateScoreTemplateDto dto, int userId);

    /// <summary>
    /// Update a score template's basic information
    /// </summary>
    Task UpdateTemplateAsync(Guid id, UpdateScoreTemplateDto dto, int userId);

    /// <summary>
    /// Publish a template (makes it immutable and available for use)
    /// </summary>
    Task PublishTemplateAsync(Guid id, int userId);

    /// <summary>
    /// Clone a template to create a new version
    /// </summary>
    Task<Guid> CloneTemplateAsync(Guid id, int userId);

    /// <summary>
    /// Archive a template (soft delete)
    /// </summary>
    Task ArchiveTemplateAsync(Guid id, int userId);
}
