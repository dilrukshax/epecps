namespace Epecps.Application.Interfaces;

/// <summary>
/// Service for synchronizing users from Azure AD to local database
/// </summary>
public interface IUserSyncService
{
    /// <summary>
    /// Sync user from Azure AD claims to local database
    /// Creates user if doesn't exist, updates if exists
    /// </summary>
    Task<int> SyncUserFromClaimsAsync(string email, string fullName, CancellationToken cancellationToken = default);
}
