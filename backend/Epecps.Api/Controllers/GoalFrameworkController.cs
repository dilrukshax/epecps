using Epecps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Epecps.Api.Controllers;

/// <summary>
/// Controller for browsing the goal framework (read-only for employees)
/// </summary>
[ApiController]
[Route("api/goal-framework")]
[Authorize]
public class GoalFrameworkController : ControllerBase
{
    private readonly IGoalFrameworkService _goalFrameworkService;

    public GoalFrameworkController(IGoalFrameworkService goalFrameworkService)
    {
        _goalFrameworkService = goalFrameworkService;
    }

    /// <summary>
    /// Get all published, non-archived templates available for goal-setting
    /// </summary>
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _goalFrameworkService.GetTemplatesAsync();
        return Ok(templates);
    }

    /// <summary>
    /// Get all active categories for a specific template
    /// </summary>
    [HttpGet("templates/{templateId}/categories")]
    public async Task<IActionResult> GetCategoriesByTemplate(Guid templateId)
    {
        var categories = await _goalFrameworkService.GetCategoriesByTemplateAsync(templateId);
        return Ok(categories);
    }

    /// <summary>
    /// Get all active categories available for goal-setting
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _goalFrameworkService.GetCategoriesAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Get all active items for a given category
    /// </summary>
    [HttpGet("categories/{categoryId}/items")]
    public async Task<IActionResult> GetItemsByCategory(Guid categoryId)
    {
        var items = await _goalFrameworkService.GetItemsByCategoryAsync(categoryId);
        return Ok(items);
    }

    /// <summary>
    /// Get all active goal items for a given item, including suggested activities
    /// </summary>
    [HttpGet("items/{itemId}/goal-items")]
    public async Task<IActionResult> GetGoalItemsByItem(Guid itemId)
    {
        var goalItems = await _goalFrameworkService.GetGoalItemsByItemAsync(itemId);
        return Ok(goalItems);
    }
}
