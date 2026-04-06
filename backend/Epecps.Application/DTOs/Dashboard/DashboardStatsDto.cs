namespace Epecps.Application.DTOs.Dashboard;

/// <summary>
/// Dashboard statistics for authorization-level users (RM, TL, HOD, GM)
/// </summary>
public class DashboardStatsDto
{
    // Approval Statistics
    public int PendingMyApproval { get; set; }
    public int TotalEvaluationsUnderReview { get; set; }
    public int CompletedThisMonth { get; set; }
    public int RejectedThisMonth { get; set; }
    
    // Goal Statistics
    public int EmployeesWithPendingGoals { get; set; }
    public int TotalGoalsUnderReview { get; set; }
    public int GoalsApprovedThisMonth { get; set; }
    public int GoalsReturnedThisMonth { get; set; }
    
    // Score Statistics
    public decimal AverageScore { get; set; }
    public int HighPerformers { get; set; } // Score >= 85
    public int LowPerformers { get; set; } // Score < 50
    public int PromotionCandidates { get; set; } // Score >= 85
    
    // Role-Specific Statistics
    public RoleSpecificStatsDto? RoleStats { get; set; }
    
    // Trend Data (for charts)
    public List<TrendDataPointDto> ApprovalTrend { get; set; } = new();
    public List<ScoreDistributionDto> ScoreDistribution { get; set; } = new();
    public List<StatusBreakdownDto> StatusBreakdown { get; set; } = new();
}

/// <summary>
/// Role-specific dashboard statistics
/// </summary>
public class RoleSpecificStatsDto
{
    public string Role { get; set; } = string.Empty;
    
    // RM-specific
    public int? DirectReports { get; set; }
    public int? GoalSetsAwaitingReview { get; set; }
    public int? GoalSetsApprovedThisWeek { get; set; }
    
    // TL-specific
    public int? TeamMembersCount { get; set; }
    public int? PeerAssignmentsPending { get; set; }
    public int? EvaluationsReadyForPeers { get; set; }
    
    // HOD-specific
    public int? DepartmentSize { get; set; }
    public int? PromotionRecommendationsPending { get; set; }
    public decimal? DepartmentAverageScore { get; set; }
    
    // GM-specific
    public int? TotalEmployees { get; set; }
    public int? PendingPromotionDecisions { get; set; }
    public int? PromotionsApprovedThisQuarter { get; set; }
}

/// <summary>
/// Trend data point for charts
/// </summary>
public class TrendDataPointDto
{
    public string Label { get; set; } = string.Empty; // Date or period label
    public int Value { get; set; }
    public string Category { get; set; } = string.Empty; // Approved, Rejected, Pending, etc.
}

/// <summary>
/// Score distribution data for charts
/// </summary>
public class ScoreDistributionDto
{
    public string Range { get; set; } = string.Empty; // "0-50", "51-70", "71-84", "85-100"
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Status breakdown data
/// </summary>
public class StatusBreakdownDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
    public string Color { get; set; } = string.Empty; // For chart colors
}

/// <summary>
/// Latest activity item for dashboard
/// </summary>
public class LatestActivityDto
{
    public int EvaluationId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime Timestamp { get; set; }
    public bool RequiresMyAction { get; set; }
    public decimal? OverallScore { get; set; }
}

/// <summary>
/// Comprehensive dashboard data
/// </summary>
public class DashboardDataDto
{
    public DashboardStatsDto Stats { get; set; } = new();
    public List<LatestActivityDto> LatestActivities { get; set; } = new();
    public List<LatestActivityDto> RecentApprovals { get; set; } = new();
    public string UserRole { get; set; } = string.Empty;
    public List<string> UserRoles { get; set; } = new();
}
