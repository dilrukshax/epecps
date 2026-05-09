using Epecps.Application.DTOs.Admin;
using Epecps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Epecps.Api.Controllers;

[ApiController]
[Route("api/v1/admin/projects")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminProjectsController : ControllerBase
{
    private readonly IAdminProjectService _projectService;

    public AdminProjectsController(IAdminProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _projectService.GetAllProjectsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _projectService.GetProjectByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
    {
        try
        {
            var result = await _projectService.CreateProjectAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.ProjectId }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectDto dto)
    {
        try
        {
            var result = await _projectService.UpdateProjectAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _projectService.DeleteProjectAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/tl")]
    public async Task<IActionResult> AssignTechLead(int id, [FromBody] AssignTlDto dto)
    {
        try
        {
            await _projectService.AssignTechLeadAsync(id, dto.UserId);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}/tl/{userId}")]
    public async Task<IActionResult> RemoveTechLead(int id, int userId)
    {
        try
        {
            await _projectService.RemoveTechLeadAsync(id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
