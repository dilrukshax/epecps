using Epecps.Application.DTOs.Admin;
using Epecps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Epecps.Api.Controllers;

[ApiController]
[Route("api/v1/admin/departments")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminDepartmentsController : ControllerBase
{
    private readonly IAdminDepartmentService _departmentService;

    public AdminDepartmentsController(IAdminDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _departmentService.GetAllDepartmentsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _departmentService.GetDepartmentByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto)
    {
        try
        {
            var result = await _departmentService.CreateDepartmentAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.DeptId }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentDto dto)
    {
        try
        {
            var result = await _departmentService.UpdateDepartmentAsync(id, dto);
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
            await _departmentService.DeleteDepartmentAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/hod")]
    public async Task<IActionResult> AssignHod(int id, [FromBody] AssignHodDto dto)
    {
        try
        {
            await _departmentService.AssignHodAsync(id, dto.UserId);
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

    [HttpDelete("{id}/hod/{userId}")]
    public async Task<IActionResult> RemoveHod(int id, int userId)
    {
        try
        {
            await _departmentService.RemoveHodAsync(id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
