using Epecps.Domain.Entities;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Epecps.Infrastructure.Data;

/// <summary>
/// Seeds initial data into the database
/// </summary>
public class DatabaseSeeder
{
    private readonly EpecpsDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(EpecpsDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds all required data
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            await SeedRolesAsync();
            await SeedDefaultCycleAsync();

            _logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    /// <summary>
    /// Seeds the system roles
    /// </summary>
    private async Task SeedRolesAsync()
    {
        _logger.LogInformation("Seeding roles...");

        var roles = new List<Role>
        {
            new Role { Name = "Employee" },
            new Role { Name = "RM" },      // Reporting Manager
            new Role { Name = "TL" },      // Team Lead
            new Role { Name = "Peer" },    // Peer Reviewer
            new Role { Name = "HOD" },     // Head of Department
            new Role { Name = "GM" },      // General Manager
            new Role { Name = "HR" },      // Human Resources
            new Role { Name = "Admin" }    // System Administrator
        };

        foreach (var role in roles)
        {
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == role.Name);

            if (existingRole == null)
            {
                _context.Roles.Add(role);
                _logger.LogInformation("Added role: {RoleName}", role.Name);
            }
            else
            {
                _logger.LogInformation("Role already exists: {RoleName}", role.Name);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Roles seeding completed.");
    }

    /// <summary>
    /// Seeds a default evaluation cycle for the current year
    /// </summary>
    private async Task SeedDefaultCycleAsync()
    {
        _logger.LogInformation("Seeding default cycle...");

        var currentYear = DateTime.UtcNow.Year;
        var cycleName = $"Cycle {currentYear}";

        var existingCycle = await _context.Set<Cycle>()
            .FirstOrDefaultAsync(c => c.Name == cycleName);

        if (existingCycle == null)
        {
            var cycle = new Cycle
            {
                Name = cycleName,
                StartDate = new DateTime(currentYear, 1, 1),
                EndDate = new DateTime(currentYear, 12, 31),
                Status = "Active"
            };

            _context.Set<Cycle>().Add(cycle);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added default cycle: {CycleName}", cycleName);
        }
        else
        {
            _logger.LogInformation("Default cycle already exists: {CycleName}", cycleName);
        }
    }

    /// <summary>
    /// Assigns all roles to a specific user (for testing purposes)
    /// </summary>
    /// <param name="userEmail">The email of the test user</param>
    public async Task AssignAllRolesToUserAsync(string userEmail)
    {
        _logger.LogInformation("Assigning all roles to user: {Email}", userEmail);

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == userEmail);

        if (user == null)
        {
            _logger.LogWarning("User not found: {Email}. User must log in first to be created.", userEmail);
            return;
        }

        var allRoles = await _context.Roles.ToListAsync();

        foreach (var role in allRoles)
        {
            var existingUserRole = user.UserRoles
                .FirstOrDefault(ur => ur.RoleId == role.RoleId);

            if (existingUserRole == null)
            {
                var userRole = new UserRole
                {
                    UserId = user.UserId,
                    RoleId = role.RoleId
                };

                _context.Set<UserRole>().Add(userRole);
                _logger.LogInformation("Assigned role {RoleName} to user {Email}", role.Name, userEmail);
            }
            else
            {
                _logger.LogInformation("User {Email} already has role {RoleName}", userEmail, role.Name);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Completed assigning all roles to user: {Email}", userEmail);
    }
}
