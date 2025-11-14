using Epecps.Application.DTOs.ScoreTemplates;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// Service implementation for managing score items
/// </summary>
public class ScoreItemService : IScoreItemService
{
    private readonly EpecpsDbContext _context;

    public ScoreItemService(EpecpsDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> CreateItemAsync(Guid categoryId, CreateScoreItemDto dto, int userId)
    {
        var category = await _context.ScoreCategories
            .Include(c => c.Template)
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        if (category == null)
            throw new NotFoundException(nameof(ScoreCategory), categoryId);

        if (category.Template.IsPublished)
            throw new BusinessRuleException("Cannot add items to a published template.");

        if (category.Template.IsArchived)
            throw new BusinessRuleException("Cannot add items to an archived template.");

        var item = new ScoreItem
        {
            Id = Guid.NewGuid(),
            ScoreCategoryId = categoryId,
            Name = dto.Name,
            Description = dto.Description,
            ItemType = dto.ItemType,
            MaxScore = dto.MaxScore,
            WeightWithinCategory = dto.WeightWithinCategory,
            IsMandatory = dto.IsMandatory,
            EvidenceRequired = dto.EvidenceRequired,
            EvidenceHint = dto.EvidenceHint,
            DisplayOrder = dto.DisplayOrder,
            IsActive = true
        };

        _context.ScoreItems.Add(item);
        
        category.Template.UpdatedAt = DateTime.UtcNow;
        category.Template.UpdatedByUserId = userId;

        await _context.SaveChangesAsync();

        return item.Id;
    }

    public async Task UpdateItemAsync(Guid itemId, UpdateScoreItemDto dto, int userId)
    {
        var item = await _context.ScoreItems
            .Include(i => i.Category)
                .ThenInclude(c => c.Template)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null)
            throw new NotFoundException(nameof(ScoreItem), itemId);

        if (item.Category.Template.IsPublished)
            throw new BusinessRuleException("Cannot update items in a published template.");

        if (item.Category.Template.IsArchived)
            throw new BusinessRuleException("Cannot update items in an archived template.");

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.ItemType = dto.ItemType;
        item.MaxScore = dto.MaxScore;
        item.WeightWithinCategory = dto.WeightWithinCategory;
        item.IsMandatory = dto.IsMandatory;
        item.EvidenceRequired = dto.EvidenceRequired;
        item.EvidenceHint = dto.EvidenceHint;
        item.DisplayOrder = dto.DisplayOrder;
        item.IsActive = dto.IsActive;

        item.Category.Template.UpdatedAt = DateTime.UtcNow;
        item.Category.Template.UpdatedByUserId = userId;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteItemAsync(Guid itemId, int userId)
    {
        var item = await _context.ScoreItems
            .Include(i => i.Category)
                .ThenInclude(c => c.Template)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null)
            throw new NotFoundException(nameof(ScoreItem), itemId);

        if (item.Category.Template.IsPublished)
        {
            // Soft delete for published templates
            item.IsActive = false;
            item.Category.Template.UpdatedAt = DateTime.UtcNow;
            item.Category.Template.UpdatedByUserId = userId;
        }
        else
        {
            // Hard delete for draft templates
            _context.ScoreItems.Remove(item);
            item.Category.Template.UpdatedAt = DateTime.UtcNow;
            item.Category.Template.UpdatedByUserId = userId;
        }

        await _context.SaveChangesAsync();
    }
}
