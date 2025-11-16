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
    /// </summary>
    public async Task<int> SyncUserFromClaimsAsync(string email, string fullName, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
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
        }
        else if (user.FullName != fullName)
        {
            // Update full name if it has changed
            user.FullName = fullName;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return user.UserId;
    }
}
