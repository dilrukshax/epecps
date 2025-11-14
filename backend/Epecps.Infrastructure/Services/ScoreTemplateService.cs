using Epecps.Application.DTOs.ScoreTemplates;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// Service implementation for managing score templates
/// </summary>
public class ScoreTemplateService : IScoreTemplateService
{
    private readonly EpecpsDbContext _context;

    public ScoreTemplateService(EpecpsDbContext context)
    {
        _context = context;
    }

    public async Task<List<ScoreTemplateListDto>> GetAllAsync(bool includeArchived = false)
    {
        var query = _context.ScoreTemplates.AsQueryable();

        if (!includeArchived)
        {
            query = query.Where(t => !t.IsArchived);
        }

        var templates = await query
            .Include(t => t.Categories)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return templates.Select(t => new ScoreTemplateListDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Version = t.Version,
            IsPublished = t.IsPublished,
            IsArchived = t.IsArchived,
            CategoryCount = t.Categories.Count,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        }).ToList();
    }

    public async Task<ScoreTemplateDetailDto?> GetByIdAsync(Guid id)
    {
        var template = await _context.ScoreTemplates
            .Include(t => t.Categories.Where(c => c.IsActive))
                .ThenInclude(c => c.Items.Where(i => i.IsActive))
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
            return null;

        return new ScoreTemplateDetailDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Version = template.Version,
            IsPublished = template.IsPublished,
            IsArchived = template.IsArchived,
            CreatedAt = template.CreatedAt,
            CreatedByUserId = template.CreatedByUserId,
            UpdatedAt = template.UpdatedAt,
            UpdatedByUserId = template.UpdatedByUserId,
            Categories = template.Categories
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new ScoreCategoryDto
                {
                    Id = c.Id,
                    ScoreTemplateId = c.ScoreTemplateId,
                    Name = c.Name,
                    Description = c.Description,
                    WeightPercent = c.WeightPercent,
                    MaxScore = c.MaxScore,
                    DisplayOrder = c.DisplayOrder,
                    IsActive = c.IsActive,
                    Items = c.Items
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => new ScoreItemDto
                        {
                            Id = i.Id,
                            ScoreCategoryId = i.ScoreCategoryId,
                            Name = i.Name,
                            Description = i.Description,
                            ItemType = i.ItemType,
                            MaxScore = i.MaxScore,
                            WeightWithinCategory = i.WeightWithinCategory,
                            IsMandatory = i.IsMandatory,
                            EvidenceRequired = i.EvidenceRequired,
                            EvidenceHint = i.EvidenceHint,
                            DisplayOrder = i.DisplayOrder,
                            IsActive = i.IsActive
                        }).ToList()
                }).ToList()
        };
    }

    public async Task<Guid> CreateTemplateAsync(CreateScoreTemplateDto dto, int userId)
    {
        var template = new ScoreTemplate
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Version = 1,
            IsPublished = false,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _context.ScoreTemplates.Add(template);
        await _context.SaveChangesAsync();

        return template.Id;
    }

    public async Task UpdateTemplateAsync(Guid id, UpdateScoreTemplateDto dto, int userId)
    {
        var template = await _context.ScoreTemplates.FindAsync(id);

        if (template == null)
            throw new NotFoundException(nameof(ScoreTemplate), id);

        if (template.IsPublished)
            throw new BusinessRuleException("Cannot update a published template. Please clone it to create a new version.");

        template.Name = dto.Name;
        template.Description = dto.Description;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = userId;

        await _context.SaveChangesAsync();
    }

    public async Task PublishTemplateAsync(Guid id, int userId)
    {
        var template = await _context.ScoreTemplates
            .Include(t => t.Categories.Where(c => c.IsActive))
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
            throw new NotFoundException(nameof(ScoreTemplate), id);

        if (template.IsPublished)
            throw new BusinessRuleException("Template is already published.");

        if (template.IsArchived)
            throw new BusinessRuleException("Cannot publish an archived template.");

        // Validate that category weights sum to 100%
        var activeCategories = template.Categories.Where(c => c.IsActive).ToList();
        
        if (!activeCategories.Any())
            throw new ValidationException("Cannot publish a template without any active categories.");

        var totalWeight = activeCategories.Sum(c => c.WeightPercent);
        if (Math.Abs(totalWeight - 100) > 0.01m) // Allow small floating point differences
            throw new ValidationException($"Category weights must sum to 100%. Current total: {totalWeight}%");

        template.IsPublished = true;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = userId;

        await _context.SaveChangesAsync();
    }

    public async Task<Guid> CloneTemplateAsync(Guid id, int userId)
    {
        var sourceTemplate = await _context.ScoreTemplates
            .Include(t => t.Categories)
                .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (sourceTemplate == null)
            throw new NotFoundException(nameof(ScoreTemplate), id);

        var newTemplate = new ScoreTemplate
        {
            Id = Guid.NewGuid(),
            Name = $"{sourceTemplate.Name} (Copy)",
            Description = sourceTemplate.Description,
            Version = sourceTemplate.Version + 1,
            IsPublished = false,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        // Clone categories and items
        foreach (var category in sourceTemplate.Categories)
        {
            var newCategory = new ScoreCategory
            {
                Id = Guid.NewGuid(),
                ScoreTemplateId = newTemplate.Id,
                Name = category.Name,
                Description = category.Description,
                WeightPercent = category.WeightPercent,
                MaxScore = category.MaxScore,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive
            };

            foreach (var item in category.Items)
            {
                var newItem = new ScoreItem
                {
                    Id = Guid.NewGuid(),
                    ScoreCategoryId = newCategory.Id,
                    Name = item.Name,
                    Description = item.Description,
                    ItemType = item.ItemType,
                    MaxScore = item.MaxScore,
                    WeightWithinCategory = item.WeightWithinCategory,
                    IsMandatory = item.IsMandatory,
                    EvidenceRequired = item.EvidenceRequired,
                    EvidenceHint = item.EvidenceHint,
                    DisplayOrder = item.DisplayOrder,
                    IsActive = item.IsActive
                };

                newCategory.Items.Add(newItem);
            }

            newTemplate.Categories.Add(newCategory);
        }

        _context.ScoreTemplates.Add(newTemplate);
        await _context.SaveChangesAsync();

        return newTemplate.Id;
    }

    public async Task ArchiveTemplateAsync(Guid id, int userId)
    {
        var template = await _context.ScoreTemplates.FindAsync(id);

        if (template == null)
            throw new NotFoundException(nameof(ScoreTemplate), id);

        if (template.IsArchived)
            throw new BusinessRuleException("Template is already archived.");

        template.IsArchived = true;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByUserId = userId;

        await _context.SaveChangesAsync();
    }
}
