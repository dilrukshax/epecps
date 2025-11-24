using Epecps.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Epecps.Api.Controllers;

/// <summary>
/// Controller for database administration tasks (development/testing only)
/// </summary>
[ApiController]
[Route("api/admin/database")]
[Authorize] // In production, add [Authorize(Roles = "Admin")]
public class DatabaseAdminController : ControllerBase
{
    private readonly DatabaseSeeder _seeder;
    private readonly ILogger<DatabaseAdminController> _logger;

    public DatabaseAdminController(
        DatabaseSeeder seeder,
        ILogger<DatabaseAdminController> logger)
    {
        _seeder = seeder;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with initial data (roles, cycles)
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> SeedDatabase()
    {
        try
        {
            _logger.LogInformation("Database seeding requested by user");
            await _seeder.SeedAsync();
            return Ok(new { message = "Database seeded successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            return StatusCode(500, new { error = "Failed to seed database", details = ex.Message });
        }
    }

    /// <summary>
    /// Assigns all roles to the current authenticated user (for testing)
    /// </summary>
    [HttpPost("assign-all-roles-to-me")]
    public async Task<IActionResult> AssignAllRolesToCurrentUser()
    {
        try
        {
            var email = User.FindFirst("preferred_username")?.Value
                ?? User.FindFirst("email")?.Value
                ?? User.FindFirst("upn")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type.Contains("email"))?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { error = "Could not determine user email from authentication token" });
            }

            _logger.LogInformation("Assigning all roles to user: {Email}", email);
            await _seeder.AssignAllRolesToUserAsync(email);

            return Ok(new
            {
                message = $"All roles assigned successfully to {email}",
                email,
                roles = new[] { "Employee", "RM", "TL", "Peer", "HOD", "GM", "HR", "Admin" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning roles");
            return StatusCode(500, new { error = "Failed to assign roles", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets current database status and seeding information
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetDatabaseStatus()
    {
        return Ok(new
        {
            message = "Database admin endpoints are available",
            endpoints = new
            {
                seed = "POST /api/admin/database/seed - Seeds roles and default cycle",
                assignRoles = "POST /api/admin/database/assign-all-roles-to-me - Assigns all roles to current user"
            }
        });
    }
}
