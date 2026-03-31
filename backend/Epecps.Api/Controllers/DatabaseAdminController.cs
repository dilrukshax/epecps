using Epecps.Infrastructure.Data;
using Epecps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Api.Controllers;

/// <summary>
/// Controller for database administration tasks (development/testing only)
/// </summary>
[ApiController]
[Route("api/admin/database")]
[Authorize(Roles = "SuperAdmin")]
public class DatabaseAdminController : ControllerBase
{
    private readonly DatabaseSeeder _seeder;
    private readonly EpecpsDbContext _context;
    private readonly ILogger<DatabaseAdminController> _logger;

    public DatabaseAdminController(
        DatabaseSeeder seeder,
        EpecpsDbContext context,
        ILogger<DatabaseAdminController> logger)
    {
        _seeder = seeder;
        _context = context;
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
                roles = new[] { "Employee", "RM", "TL", "Peer", "HOD", "GM", "HR", "Admin", "SuperAdmin" }
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
    public async Task<IActionResult> GetDatabaseStatus()
    {
        try
        {
            var userCount = await _context.Users.CountAsync();
            var roleCount = await _context.Roles.CountAsync();
            var cycleCount = await _context.Set<Domain.Entities.Cycle>().CountAsync();
            var userRoleCount = await _context.Set<Domain.Entities.UserRole>().CountAsync();

            return Ok(new
            {
                message = "Database admin endpoints are available",
                statistics = new
                {
                    users = userCount,
                    roles = roleCount,
                    cycles = cycleCount,
                    userRoleAssignments = userRoleCount
                },
                endpoints = new
                {
                    seed = "POST /api/admin/database/seed - Seeds roles and default cycle",
                    assignRoles = "POST /api/admin/database/assign-all-roles-to-me - Assigns all roles to current user",
                    getUsers = "GET /api/admin/database/users - Get all users with their roles",
                    getUserRoles = "GET /api/admin/database/users/{userId}/roles - Get roles for specific user",
                    assignRole = "POST /api/admin/database/users/{userId}/roles - Assign role to user",
                    removeRole = "DELETE /api/admin/database/users/{userId}/roles/{roleId} - Remove role from user"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database status");
            return StatusCode(500, new { error = "Failed to get database status", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all users with their assigned roles
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Department)
                .Select(u => new
                {
                    userId = u.UserId,
                    fullName = u.FullName,
                    email = u.Email,
                    status = u.Status,
                    department = u.Department != null ? u.Department.Name : "No Department",
                    departmentId = u.DeptId,
                    roles = u.UserRoles.Select(ur => new
                    {
                        roleId = ur.RoleId,
                        roleName = ur.Role.Name
                    }).ToList()
                })
                .OrderBy(u => u.fullName)
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new { error = "Failed to get users", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all available roles
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetAllRoles()
    {
        try
        {
            var roles = await _context.Roles
                .Select(r => new
                {
                    roleId = r.RoleId,
                    name = r.Name
                })
                .OrderBy(r => r.name)
                .ToListAsync();

            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles");
            return StatusCode(500, new { error = "Failed to get roles", details = ex.Message });
        }
    }

    /// <summary>
    /// Get roles for a specific user
    /// </summary>
    [HttpGet("users/{userId}/roles")]
    public async Task<IActionResult> GetUserRoles(int userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new { error = $"User with ID {userId} not found" });
            }

            var roles = user.UserRoles.Select(ur => new
            {
                roleId = ur.RoleId,
                roleName = ur.Role.Name
            }).ToList();

            return Ok(new
            {
                userId = user.UserId,
                fullName = user.FullName,
                email = user.Email,
                roles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user roles for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to get user roles", details = ex.Message });
        }
    }

    /// <summary>
    /// Assign a role to a user
    /// </summary>
    [HttpPost("users/{userId}/roles")]
    public async Task<IActionResult> AssignRoleToUser(int userId, [FromBody] AssignRoleRequest request)
    {
        try
        {
            if (request == null || request.RoleId <= 0)
            {
                return BadRequest(new { error = "Valid roleId is required" });
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new { error = $"User with ID {userId} not found" });
            }

            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null)
            {
                return NotFound(new { error = $"Role with ID {request.RoleId} not found" });
            }

            // Check if user already has this role
            var existingUserRole = user.UserRoles.FirstOrDefault(ur => ur.RoleId == request.RoleId);
            if (existingUserRole != null)
            {
                return BadRequest(new { error = $"User already has the role: {role.Name}" });
            }

            // Add the role
            var userRole = new Domain.Entities.UserRole
            {
                UserId = userId,
                RoleId = request.RoleId
            };

            _context.Set<Domain.Entities.UserRole>().Add(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Assigned role {RoleName} to user {Email}", role.Name, user.Email);

            return Ok(new
            {
                message = $"Role '{role.Name}' assigned to user '{user.FullName}' successfully",
                userId = user.UserId,
                roleId = role.RoleId,
                roleName = role.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to assign role", details = ex.Message });
        }
    }

    /// <summary>
    /// Remove a role from a user
    /// </summary>
    [HttpDelete("users/{userId}/roles/{roleId}")]
    public async Task<IActionResult> RemoveRoleFromUser(int userId, int roleId)
    {
        try
        {
            var userRole = await _context.Set<Domain.Entities.UserRole>()
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
            {
                return NotFound(new { error = "User does not have this role" });
            }

            _context.Set<Domain.Entities.UserRole>().Remove(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed role {RoleName} from user {Email}", userRole.Role.Name, userRole.User.Email);

            return Ok(new
            {
                message = $"Role '{userRole.Role.Name}' removed from user '{userRole.User.FullName}' successfully",
                userId,
                roleId,
                roleName = userRole.Role.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to remove role", details = ex.Message });
        }
    }

    /// <summary>
    /// Assign multiple roles to a user at once
    /// </summary>
    [HttpPost("users/{userId}/roles/bulk")]
    public async Task<IActionResult> AssignMultipleRoles(int userId, [FromBody] AssignMultipleRolesRequest request)
    {
        try
        {
            if (request == null || request.RoleIds == null || !request.RoleIds.Any())
            {
                return BadRequest(new { error = "At least one roleId is required" });
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new { error = $"User with ID {userId} not found" });
            }

            var roles = await _context.Roles
                .Where(r => request.RoleIds.Contains(r.RoleId))
                .ToListAsync();

            if (roles.Count != request.RoleIds.Count)
            {
                return BadRequest(new { error = "One or more role IDs are invalid" });
            }

            var addedRoles = new List<string>();
            var skippedRoles = new List<string>();

            foreach (var role in roles)
            {
                var existingUserRole = user.UserRoles.FirstOrDefault(ur => ur.RoleId == role.RoleId);
                if (existingUserRole == null)
                {
                    var userRole = new Domain.Entities.UserRole
                    {
                        UserId = userId,
                        RoleId = role.RoleId
                    };
                    _context.Set<Domain.Entities.UserRole>().Add(userRole);
                    addedRoles.Add(role.Name);
                }
                else
                {
                    skippedRoles.Add(role.Name);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Assigned {Count} roles to user {Email}", addedRoles.Count, user.Email);

            return Ok(new
            {
                message = $"Roles assigned to user '{user.FullName}' successfully",
                userId = user.UserId,
                addedRoles,
                skippedRoles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning multiple roles to user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to assign roles", details = ex.Message });
        }
    }
}

/// <summary>
/// Request model for assigning a role
/// </summary>
public class AssignRoleRequest
{
    public int RoleId { get; set; }
}

/// <summary>
/// Request model for assigning multiple roles
/// </summary>
public class AssignMultipleRolesRequest
{
    public List<int> RoleIds { get; set; } = new();
}
