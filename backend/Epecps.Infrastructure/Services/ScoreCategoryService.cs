using Epecps.Application.DTOs.ScoreTemplates;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// Service implementation for managing score categories
/// </summary>
public class ScoreCategoryService : IScoreCategoryService
{
    private readonly EpecpsDbContext _context;

    public ScoreCategoryService(EpecpsDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> CreateCategoryAsync(Guid templateId, CreateScoreCategoryDto dto, int userId)
    {
        var template = await _context.ScoreTemplates.FindAsync(templateId);

        if (template == null)
            throw new NotFoundException(nameof(ScoreTemplate), templateId);

        if (template.IsPublished)
            throw new BusinessRuleException("Cannot add categories to a published template.");

        if (template.IsArchived)
            throw new BusinessRuleException("Cannot add categories to an archived template.");

        var category = new ScoreCategory
        {
            Id = Guid.NewGuid(),
            ScoreTemplateId = templateId,
            Name = dto.Name,
            Description = dto.Description,
            WeightPercent = dto.WeightPercent,
            MaxScore = dto.MaxScore,
            DisplayOrder = dto.DisplayOrder,
            IsActive = true
        };

        _context.ScoreCategories.Add(category);
        
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = userId;

        await _context.SaveChangesAsync();

        return category.Id;
    }

    public async Task UpdateCategoryAsync(Guid categoryId, UpdateScoreCategoryDto dto, int userId)
    {
        var category = await _context.ScoreCategories
            .Include(c => c.Template)
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        if (category == null)
            throw new NotFoundException(nameof(ScoreCategory), categoryId);

        if (category.Template.IsPublished)
            throw new BusinessRuleException("Cannot update categories in a published template.");

        if (category.Template.IsArchived)
            throw new BusinessRuleException("Cannot update categories in an archived template.");

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.WeightPercent = dto.WeightPercent;
        category.MaxScore = dto.MaxScore;
        category.DisplayOrder = dto.DisplayOrder;
        category.IsActive = dto.IsActive;

        category.Template.UpdatedAt = DateTime.UtcNow;
        category.Template.UpdatedByUserId = userId;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(Guid categoryId, int userId)
    {
        var category = await _context.ScoreCategories
            .Include(c => c.Template)
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        if (category == null)
            throw new NotFoundException(nameof(ScoreCategory), categoryId);

        if (category.Template.IsPublished)
        {
            // Soft delete for published templates
            category.IsActive = false;
            category.Template.UpdatedAt = DateTime.UtcNow;
            category.Template.UpdatedByUserId = userId;
        }
        else
        {
            // Hard delete for draft templates
            _context.ScoreCategories.Remove(category);
            category.Template.UpdatedAt = DateTime.UtcNow;
            category.Template.UpdatedByUserId = userId;
        }

        await _context.SaveChangesAsync();
    }
}
