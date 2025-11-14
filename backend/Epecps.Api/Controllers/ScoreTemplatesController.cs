using Epecps.Application.DTOs.ScoreTemplates;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace Epecps.Api.Controllers;

/// <summary>
/// Admin-only controller for managing scoring templates
/// </summary>
[ApiController]
[Route("api/v1/admin/templates")]
// TODO: Re-enable role check after assigning Admin role in Azure AD
// [Authorize(Roles = "Admin")]
[Authorize] // Changed from [Authorize(Roles = "Admin")] to allow testing without role
[RequiredScope("Epecps.ReadWrite")]
public class ScoreTemplatesController : ControllerBase
{
    private readonly IScoreTemplateService _templateService;
    private readonly IScoreCategoryService _categoryService;
    private readonly IScoreItemService _itemService;

    public ScoreTemplatesController(
        IScoreTemplateService templateService,
        IScoreCategoryService categoryService,
        IScoreItemService itemService)
    {
        _templateService = templateService;
        _categoryService = categoryService;
        _itemService = itemService;
    }

    #region Template Management

    /// <summary>
    /// Get all score templates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ScoreTemplateListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ScoreTemplateListDto>>> GetAllTemplates([FromQuery] bool includeArchived = false)
    {
        var templates = await _templateService.GetAllAsync(includeArchived);
        return Ok(templates);
    }

    /// <summary>
    /// Get a specific template with all categories and items
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ScoreTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScoreTemplateDetailDto>> GetTemplateById(Guid id)
    {
        var template = await _templateService.GetByIdAsync(id);
        
        if (template == null)
            return NotFound(new { message = $"Template with id '{id}' was not found." });

        return Ok(template);
    }

    /// <summary>
    /// Create a new score template
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateTemplate([FromBody] CreateScoreTemplateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var templateId = await _templateService.CreateTemplateAsync(dto, userId);

        return CreatedAtAction(nameof(GetTemplateById), new { id = templateId }, templateId);
    }

    /// <summary>
    /// Update a template's basic information
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateScoreTemplateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetCurrentUserId();
            await _templateService.UpdateTemplateAsync(id, dto, userId);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Publish a template (makes it immutable and available for use)
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishTemplate(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _templateService.PublishTemplateAsync(id, userId);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message, errors = ex.Errors });
        }
    }

    /// <summary>
    /// Clone a template to create a new draft version
    /// </summary>
    [HttpPost("{id:guid}/clone")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> CloneTemplate(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var newTemplateId = await _templateService.CloneTemplateAsync(id, userId);
            return CreatedAtAction(nameof(GetTemplateById), new { id = newTemplateId }, newTemplateId);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Archive a template (soft delete)
    /// </summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveTemplate(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _templateService.ArchiveTemplateAsync(id, userId);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Category Management

    /// <summary>
    /// Create a new category within a template
    /// </summary>
    [HttpPost("{templateId:guid}/categories")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> CreateCategory(Guid templateId, [FromBody] CreateScoreCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetCurrentUserId();
            var categoryId = await _categoryService.CreateCategoryAsync(templateId, dto, userId);
            return CreatedAtAction(nameof(GetTemplateById), new { id = templateId }, categoryId);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing category
    /// </summary>
    [HttpPut("categories/{categoryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(Guid categoryId, [FromBody] UpdateScoreCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetCurrentUserId();
            await _categoryService.UpdateCategoryAsync(categoryId, dto, userId);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a category (soft delete if template is published)
    /// </summary>
    [HttpDelete("categories/{categoryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid categoryId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _categoryService.DeleteCategoryAsync(categoryId, userId);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region Score Item Management

    /// <summary>
    /// Create a new item within a category
    /// </summary>
    [HttpPost("categories/{categoryId:guid}/items")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> CreateItem(Guid categoryId, [FromBody] CreateScoreItemDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetCurrentUserId();
            var itemId = await _itemService.CreateItemAsync(categoryId, dto, userId);
            return Created($"/api/v1/admin/items/{itemId}", itemId);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing score item
    /// </summary>
    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateScoreItemDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetCurrentUserId();
            await _itemService.UpdateItemAsync(itemId, dto, userId);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a score item (soft delete if template is published)
    /// </summary>
    [HttpDelete("items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(Guid itemId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _itemService.DeleteItemAsync(itemId, userId);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get the current user's ID from the JWT token
    /// For now, returns a dummy value - should extract from User claims
    /// </summary>
    private int GetCurrentUserId()
    {
        // TODO: Extract from User.FindFirst("oid") or custom claim
        // For now, return a placeholder
        var userIdClaim = User.FindFirst("oid")?.Value;
        
        // If you're storing integer user IDs, you'll need to map the Azure AD object ID
        // to your internal user ID. For now, using a dummy value.
        return 1; // Replace with actual user ID mapping logic
    }

    #endregion
}
