using Epecps.Application.DTOs.Dashboard;

namespace Epecps.Application.Interfaces;

/// <summary>
/// Service for dashboard statistics and data
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Get comprehensive dashboard data for the current user
    /// </summary>
    Task<DashboardDataDto> GetDashboardDataAsync(int userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get statistics for the dashboard
    /// </summary>
    Task<DashboardStatsDto> GetDashboardStatsAsync(int userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get latest activities (pending and recent approvals)
    /// </summary>
    Task<List<LatestActivityDto>> GetLatestActivitiesAsync(int userId, int count = 10, CancellationToken cancellationToken = default);
}
