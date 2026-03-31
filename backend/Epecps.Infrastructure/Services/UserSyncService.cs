using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// Service for synchronizing users by email claim to local database
/// </summary>
public class UserSyncService : IUserSyncService
{
    private readonly EpecpsDbContext _context;

    public UserSyncService(EpecpsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Sync user by claims to local database
    /// Creates user if doesn't exist, updates if exists
    /// </summary>
    public async Task<int> SyncUserFromClaimsAsync(string email, string fullName, CancellationToken cancellationToken = default)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null)
        {
            var departmentId = await ResolveDepartmentIdAsync(cancellationToken);

            user = new User
            {
                Email = email,
                FullName = fullName,
                Status = "Active",
                DeptId = departmentId,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            await EnsureRoleAsync(user.UserId, "Employee", cancellationToken);
        }
        else
        {
            // Update full name if it has changed
            if (!string.IsNullOrWhiteSpace(fullName) && user.FullName != fullName)
            {
                user.FullName = fullName;
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
                user.Status = "Active";
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return user.UserId;
    }

    private async Task EnsureRoleAsync(int userId, string roleName, CancellationToken cancellationToken)
    {
        var role = await _context.Set<Role>()
            .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);

        if (role == null)
        {
            role = new Role { Name = roleName };
            _context.Set<Role>().Add(role);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var existingUserRole = await _context.Set<UserRole>()
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.RoleId, cancellationToken);

        if (!existingUserRole)
        {
            _context.Set<UserRole>().Add(new UserRole
            {
                UserId = userId,
                RoleId = role.RoleId
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<int> ResolveDepartmentIdAsync(CancellationToken cancellationToken)
    {
        var department = await _context.Departments.OrderBy(d => d.DeptId).FirstOrDefaultAsync(cancellationToken);
        if (department != null)
        {
            return department.DeptId;
        }

        department = new Department { Name = "General" };
        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);
        return department.DeptId;
    }
}
