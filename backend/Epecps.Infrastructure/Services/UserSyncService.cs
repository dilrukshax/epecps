using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// Service for synchronizing users from Azure AD to local database
/// </summary>
public class UserSyncService : IUserSyncService
{
    private readonly EpecpsDbContext _context;

    public UserSyncService(EpecpsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Sync user from Azure AD claims to local database
    /// Creates user if doesn't exist, updates if exists
    /// For new users, assigns all roles to allow testing the full workflow
    /// </summary>
    public async Task<int> SyncUserFromClaimsAsync(string email, string fullName, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null)
        {
            // Create new user with default department (DeptId = 1) and active status
            user = new User
            {
                Email = email,
                FullName = fullName,
                Status = "Active",
                DeptId = 1 // Default department - you may want to make this configurable
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            // Assign all roles to the new user for testing purposes
            // This allows a single user to test the complete approval workflow
            await AssignAllRolesToUserAsync(user.UserId, cancellationToken);
        }
        else
        {
            // Update full name if it has changed
            if (user.FullName != fullName)
            {
                user.FullName = fullName;
            }

            // Ensure user has all required roles (for testing purposes)
            // Check if user already has roles assigned
            if (user.UserRoles == null || !user.UserRoles.Any())
            {
                await AssignAllRolesToUserAsync(user.UserId, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return user.UserId;
    }

    /// <summary>
    /// Assigns all workflow roles to a user for testing purposes
    /// In production, this should be replaced with proper role management
    /// </summary>
    private async Task AssignAllRolesToUserAsync(int userId, CancellationToken cancellationToken)
    {
        // Get or create all required roles
        var roleNames = new[] { "Employee", "RM", "TL", "Peer", "HOD", "GM", "HR", "Admin" };
        
        foreach (var roleName in roleNames)
        {
            // Find or create the role
            var role = await _context.Set<Role>()
                .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);

            if (role == null)
            {
                role = new Role { Name = roleName };
                _context.Set<Role>().Add(role);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Check if user already has this role
            var existingUserRole = await _context.Set<UserRole>()
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.RoleId, cancellationToken);

            if (existingUserRole == null)
            {
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = role.RoleId
                };
                _context.Set<UserRole>().Add(userRole);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
