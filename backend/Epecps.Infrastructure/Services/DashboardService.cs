using Epecps.Application.DTOs.Dashboard;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Domain.Enums;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// Service for dashboard statistics and data
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly EpecpsDbContext _context;

    public DashboardService(EpecpsDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDataDto> GetDashboardDataAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        
        var stats = await GetDashboardStatsAsync(userId, cancellationToken);
        var latestActivities = await GetLatestActivitiesAsync(userId, 15, cancellationToken);
        var recentApprovals = latestActivities
            .Where(a => !a.RequiresMyAction && (a.Action.Contains("Approved") || a.Action.Contains("Completed")))
            .Take(10)
            .ToList();

        return new DashboardDataDto
        {
            Stats = stats,
            LatestActivities = latestActivities.Where(a => a.RequiresMyAction).Take(10).ToList(),
            RecentApprovals = recentApprovals,
            UserRole = userRoles.FirstOrDefault() ?? "Employee",
            UserRoles = userRoles
        };
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        var now = DateTime.UtcNow;
        var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
        
        // Get all evaluations visible to the user based on their role
        var visibleEvaluations = await GetVisibleEvaluationsAsync(userId, userRoles, cancellationToken);
        
        // Pending my approval
        var pendingMyApproval = await GetPendingMyApprovalCountAsync(userId, userRoles, cancellationToken);
        
        // Total under review (not completed)
        var underReview = visibleEvaluations.Count(e =>
            !e.Status.Contains("completed", StringComparison.OrdinalIgnoreCase) &&
            !e.Status.Contains("rejected", StringComparison.OrdinalIgnoreCase));
        
        // Completed this month
        var completedThisMonth = await _context.Set<ApprovalHistory>()
            .Where(ah => ah.CreatedAt >= firstDayOfMonth)
            .Where(ah => ah.Action.Contains("Completed") || ah.ToStatus.Contains("Completed"))
            .Select(ah => ah.EvaluationId)
            .Distinct()
            .CountAsync(cancellationToken);
        
        // Rejected this month
        var rejectedThisMonth = await _context.Set<ApprovalHistory>()
            .Where(ah => ah.CreatedAt >= firstDayOfMonth)
            .Where(ah => ah.Action.Contains("Rejected"))
            .Select(ah => ah.EvaluationId)
            .Distinct()
            .CountAsync(cancellationToken);
        
        // Goal statistics
        var pendingGoalCount = await _context.PersonalGoals
            .Where(pg => pg.Status == PersonalGoalStatus.PendingRMReview || pg.Status == PersonalGoalStatus.ApprovedByRM)
            .CountAsync(cancellationToken);
        
        var employeesWithPendingGoals = await _context.PersonalGoals
            .Where(pg => pg.Status == PersonalGoalStatus.PendingRMReview || pg.Status == PersonalGoalStatus.ApprovedByRM)
            .Select(pg => pg.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
        
        var goalsApprovedThisMonth = await _context.PersonalGoals
            .Where(pg => pg.UpdatedAt >= firstDayOfMonth && pg.Status == PersonalGoalStatus.ApprovedByRM)
            .CountAsync(cancellationToken);
        
        var goalsReturnedThisMonth = await _context.PersonalGoals
            .Where(pg => pg.UpdatedAt >= firstDayOfMonth && pg.Status == PersonalGoalStatus.ReturnedToEmployee)
            .CountAsync(cancellationToken);
        
        // Score statistics
        var evaluationsWithScores = visibleEvaluations.Where(e => e.OverallScore.HasValue && e.OverallScore.Value > 0).ToList();
        var avgScore = evaluationsWithScores.Any() ? evaluationsWithScores.Average(e => e.OverallScore!.Value) : 0;
        var highPerformers = evaluationsWithScores.Count(e => e.OverallScore >= 85);
        var lowPerformers = evaluationsWithScores.Count(e => e.OverallScore < 50);
        var promotionCandidates = evaluationsWithScores.Count(e => e.OverallScore >= 85);
        
        // Trend data (last 7 days)
        var approvalTrend = await GetApprovalTrendDataAsync(userId, userRoles, 7, cancellationToken);
        
        // Score distribution
        var scoreDistribution = GetScoreDistributionData(evaluationsWithScores);
        
        // Status breakdown
        var statusBreakdown = GetStatusBreakdownData(visibleEvaluations);
        
        // Role-specific stats
        var roleStats = await GetRoleSpecificStatsAsync(userId, userRoles, cancellationToken);
        
        return new DashboardStatsDto
        {
            PendingMyApproval = pendingMyApproval,
            TotalEvaluationsUnderReview = underReview,
            CompletedThisMonth = completedThisMonth,
            RejectedThisMonth = rejectedThisMonth,
            EmployeesWithPendingGoals = employeesWithPendingGoals,
            TotalGoalsUnderReview = pendingGoalCount,
            GoalsApprovedThisMonth = goalsApprovedThisMonth,
            GoalsReturnedThisMonth = goalsReturnedThisMonth,
            AverageScore = Math.Round(avgScore, 2),
            HighPerformers = highPerformers,
            LowPerformers = lowPerformers,
            PromotionCandidates = promotionCandidates,
            RoleStats = roleStats,
            ApprovalTrend = approvalTrend,
            ScoreDistribution = scoreDistribution,
            StatusBreakdown = statusBreakdown
        };
    }

    public async Task<List<LatestActivityDto>> GetLatestActivitiesAsync(int userId, int count = 10, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        
        // Get latest approval history entries - execute the query first
        var historyEntries = await _context.Set<ApprovalHistory>()
            .Include(ah => ah.Evaluation)
                .ThenInclude(e => e.Employee)
            .Include(ah => ah.ActorUser)
            .OrderByDescending(ah => ah.CreatedAt)
            .Take(count * 3) // Get more to filter
            .ToListAsync(cancellationToken);
        
        // Process in memory to determine RequiresMyAction
        var activities = historyEntries.Select(ah => new LatestActivityDto
        {
            EvaluationId = ah.EvaluationId,
            EmployeeName = ah.Evaluation.Employee.FullName,
            EmployeeEmail = ah.Evaluation.Employee.Email,
            Status = ah.ToStatus,
            Action = ah.Action,
            ActorName = ah.ActorUser.FullName,
            ActorRole = ah.ActorRole,
            Comment = ah.Comment,
            Timestamp = ah.CreatedAt,
            RequiresMyAction = DetermineIfRequiresMyAction(ah.ToStatus, userId, userRoles, ah.Evaluation),
            OverallScore = ah.Evaluation.OverallScore
        }).ToList();
        
        return activities.Take(count).ToList();
    }

    // Helper methods
    
    private async Task<List<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.Set<UserRole>()
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Evaluation>> GetVisibleEvaluationsAsync(int userId, List<string> userRoles, CancellationToken cancellationToken)
    {
        var query = _context.Set<Evaluation>()
            .Include(e => e.Employee)
            .Include(e => e.Reviews)
            .Include(e => e.PeerAssignments)
            .AsQueryable();

        var managedEmployeeIds = userRoles.Contains("RM")
            ? await _context.UserManagerMappings
                .Where(m => m.ManagerUserId == userId)
                .Select(m => m.EmployeeUserId)
                .Distinct()
                .ToListAsync(cancellationToken)
            : new List<int>();

        var hodDeptIds = userRoles.Contains("HOD")
            ? await _context.DepartmentHodMappings
                .Where(m => m.HodUserId == userId)
                .Select(m => m.DeptId)
                .Distinct()
                .ToListAsync(cancellationToken)
            : new List<int>();

        // Filter based on roles
        if (userRoles.Contains("GM") || userRoles.Contains("HR"))
        {
            // Can see all evaluations
        }
        else if (userRoles.Contains("HOD"))
        {
            if (hodDeptIds.Count > 0)
            {
                query = query.Where(e => hodDeptIds.Contains(e.Employee.DeptId));
            }
        }
        else if (userRoles.Contains("RM"))
        {
            query = query.Where(e =>
                e.ReportingManagerId == userId ||
                managedEmployeeIds.Contains(e.EmployeeId) ||
                e.EmployeeId == userId);
        }
        else if (userRoles.Contains("TL"))
        {
            query = query.Where(e => e.TeamLeadId == userId || e.EmployeeId == userId);
        }
        else if (userRoles.Contains("Peer"))
        {
            query = query.Where(e => e.PeerAssignments.Any(pa => pa.PeerUserId == userId) || e.EmployeeId == userId);
        }
        else
        {
            query = query.Where(e => e.EmployeeId == userId);
        }
        
        return await query.ToListAsync(cancellationToken);
    }

    private async Task<int> GetPendingMyApprovalCountAsync(int userId, List<string> userRoles, CancellationToken cancellationToken)
    {
        var count = 0;

        // Employee-facing workflow-v2 tasks
        count += await _context.Set<Evaluation>()
            .Where(e => e.EmployeeId == userId)
            .Where(e =>
                e.Status == "V2_PENDING_EMPLOYEE_ACTIVATION" ||
                e.Status == "V2_RETURNED_FOR_ACTIVATION" ||
                e.Status == "V2_ACTIVE_GOALS")
            .CountAsync(cancellationToken);

        var managedEmployeeIds = userRoles.Contains("RM")
            ? await _context.UserManagerMappings
                .Where(m => m.ManagerUserId == userId)
                .Select(m => m.EmployeeUserId)
                .Distinct()
                .ToListAsync(cancellationToken)
            : new List<int>();

        var hodDeptIds = userRoles.Contains("HOD")
            ? await _context.DepartmentHodMappings
                .Where(m => m.HodUserId == userId)
                .Select(m => m.DeptId)
                .Distinct()
                .ToListAsync(cancellationToken)
            : new List<int>();

        // RM pending
        if (userRoles.Contains("RM"))
        {
            count += await _context.Set<Evaluation>()
                .Where(e => e.ReportingManagerId == userId || managedEmployeeIds.Contains(e.EmployeeId))
                .Where(e => e.Status == "Pending_RM_Review" || e.Status == "Pending_RM_Review_PostCompletion")
                .CountAsync(cancellationToken);

            count += await _context.Set<Evaluation>()
                .Where(e => e.ReportingManagerId == userId)
                .Where(e =>
                    e.Status == "V2_PENDING_RM_ACTIVATION_REVIEW" ||
                    e.Status == "V2_PENDING_TL_ACTIVATION_REVIEW")
                .CountAsync(cancellationToken);

            count += await _context.Set<Evaluation>()
                .Include(e => e.Reviews)
                .Where(e => e.Status == "V2_PENDING_PARALLEL_REVIEWS")
                .Where(e => e.Reviews.Any(r =>
                    r.ReviewerRole == ReviewerRole.RM &&
                    r.ReviewerUserId == userId &&
                    r.Status == "Pending"))
                .CountAsync(cancellationToken);
        }

        // TL pending
        if (userRoles.Contains("TL"))
        {
            count += await _context.Set<Evaluation>()
                .Where(e => e.TeamLeadId == userId)
                .Where(e => e.Status == "Pending_TL_Review" || e.Status == "Pending_Peer_Assignment")
                .CountAsync(cancellationToken);

            count += await _context.Set<Evaluation>()
                .Include(e => e.Reviews)
                .Where(e => e.TeamLeadId == userId)
                .Where(e =>
                    e.Status == "V2_PENDING_PARALLEL_REVIEWS" &&
                     e.Reviews.Any(r =>
                         r.ReviewerRole == ReviewerRole.TL &&
                         r.ReviewerUserId == userId &&
                         r.Status == "Pending"))
                .CountAsync(cancellationToken);
        }

        // Peer pending
        if (userRoles.Contains("Peer"))
        {
            count += await _context.Set<Evaluation>()
                .Include(e => e.Reviews)
                .Include(e => e.PeerAssignments)
                .Where(e => e.Status == "Pending_Peer_Reviews")
                .Where(e => e.PeerAssignments.Any(pa => pa.PeerUserId == userId))
                .Where(e => e.Reviews.Any(r => r.ReviewerUserId == userId && r.ReviewerRole == ReviewerRole.Peer && r.Status == "Pending"))
                .CountAsync(cancellationToken);

            count += await _context.Set<Evaluation>()
                .Include(e => e.Reviews)
                .Where(e => e.Status == "V2_PENDING_PARALLEL_REVIEWS")
                .Where(e => e.Reviews.Any(r =>
                    r.ReviewerUserId == userId &&
                    r.ReviewerRole == ReviewerRole.Peer &&
                    r.Status == "Pending"))
                .CountAsync(cancellationToken);
        }

        // HOD pending
        if (userRoles.Contains("HOD"))
        {
            var hodQuery = _context.Set<Evaluation>()
                .Include(e => e.Employee)
                .Where(e => e.Status == "Pending_HOD_Review" || e.Status == "V2_PENDING_HOD_REVIEW")
                .AsQueryable();

            if (hodDeptIds.Count > 0)
            {
                hodQuery = hodQuery.Where(e => hodDeptIds.Contains(e.Employee.DeptId));
            }

            count += await hodQuery.CountAsync(cancellationToken);
        }

        // GM pending
        if (userRoles.Contains("GM"))
        {
            count += await _context.Set<Evaluation>()
                .Where(e => e.Status == "Pending_GM_Decision" || e.Status == "V2_PENDING_GM_DECISION")
                .CountAsync(cancellationToken);
        }

        // HR pending
        if (userRoles.Contains("HR"))
        {
            count += await _context.Set<Evaluation>()
                .Where(e =>
                    e.Status == "Pending_HR_Processing" ||
                    e.Status == "V2_PENDING_HR_PROMOTION" ||
                    e.Status == "V2_PENDING_HR_LOW_PERFORMER")
                .CountAsync(cancellationToken);
        }

        return count;
    }

    private async Task<List<TrendDataPointDto>> GetApprovalTrendDataAsync(int userId, List<string> userRoles, int days, CancellationToken cancellationToken)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        
        var approvals = await _context.Set<ApprovalHistory>()
            .Where(ah => ah.CreatedAt >= startDate)
            .GroupBy(ah => new { Date = ah.CreatedAt.Date, Category = ah.Action.Contains("Approved") ? "Approved" : ah.Action.Contains("Rejected") ? "Rejected" : "Other" })
            .Select(g => new TrendDataPointDto
            {
                Label = g.Key.Date.ToString("MM/dd"),
                Value = g.Count(),
                Category = g.Key.Category
            })
            .ToListAsync(cancellationToken);
        
        return approvals;
    }

    private List<ScoreDistributionDto> GetScoreDistributionData(List<Evaluation> evaluations)
    {
        var total = evaluations.Count;
        if (total == 0) return new List<ScoreDistributionDto>();
        
        var ranges = new List<(string Range, int Min, int Max)>
        {
            ("0-50", 0, 50),
            ("51-70", 51, 70),
            ("71-84", 71, 84),
            ("85-100", 85, 100)
        };
        
        return ranges.Select(r => new ScoreDistributionDto
        {
            Range = r.Range,
            Count = evaluations.Count(e => e.OverallScore >= r.Min && e.OverallScore <= r.Max),
            Percentage = Math.Round((decimal)evaluations.Count(e => e.OverallScore >= r.Min && e.OverallScore <= r.Max) / total * 100, 2)
        }).ToList();
    }

    private List<StatusBreakdownDto> GetStatusBreakdownData(List<Evaluation> evaluations)
    {
        var total = evaluations.Count;
        if (total == 0) return new List<StatusBreakdownDto>();
        
        var statusColors = new Dictionary<string, string>
        {
            { "Pending", "#3B82F6" },
            { "Approved", "#10B981" },
            { "Rejected", "#EF4444" },
            { "Completed", "#059669" },
            { "Under Review", "#F59E0B" }
        };
        
        return evaluations
            .GroupBy(e => GetStatusCategory(e.Status))
            .Select(g => new StatusBreakdownDto
            {
                Status = g.Key,
                Count = g.Count(),
                Percentage = Math.Round((decimal)g.Count() / total * 100, 2),
                Color = statusColors.ContainsKey(g.Key) ? statusColors[g.Key] : "#6B7280"
            })
            .OrderByDescending(s => s.Count)
            .ToList();
    }

    private string GetStatusCategory(string status)
    {
        var normalized = status.ToLowerInvariant();
        if (normalized.Contains("completed")) return "Completed";
        if (normalized.Contains("rejected")) return "Rejected";
        if (normalized.Contains("pending")) return "Pending";
        if (normalized.Contains("approved")) return "Approved";
        return "Under Review";
    }

    private bool DetermineIfRequiresMyAction(string status, int userId, List<string> userRoles, Evaluation evaluation)
    {
        if (userRoles.Contains("RM") && (status == "Pending_RM_Review" || status == "Pending_RM_Review_PostCompletion") && evaluation.ReportingManagerId == userId)
            return true;

        if (userRoles.Contains("RM") &&
            (status == "V2_PENDING_RM_ACTIVATION_REVIEW" || status == "V2_PENDING_TL_ACTIVATION_REVIEW") &&
            evaluation.ReportingManagerId == userId)
            return true;

        if (userRoles.Contains("RM") &&
            status == "V2_PENDING_PARALLEL_REVIEWS" &&
            evaluation.Reviews.Any(r => r.ReviewerRole == ReviewerRole.RM && r.ReviewerUserId == userId && r.Status == "Pending"))
            return true;

        if (userRoles.Contains("TL") && (status == "Pending_TL_Review" || status == "Pending_Peer_Assignment") && evaluation.TeamLeadId == userId)
            return true;

        if (userRoles.Contains("TL") &&
            status == "V2_PENDING_PARALLEL_REVIEWS" &&
            evaluation.Reviews.Any(r => r.ReviewerRole == ReviewerRole.TL && r.ReviewerUserId == userId && r.Status == "Pending") &&
            evaluation.TeamLeadId == userId)
            return true;

        if (userRoles.Contains("Peer") &&
            status == "V2_PENDING_PARALLEL_REVIEWS" &&
            evaluation.Reviews.Any(r => r.ReviewerRole == ReviewerRole.Peer && r.ReviewerUserId == userId && r.Status == "Pending"))
            return true;

        if (userRoles.Contains("HOD") && (status == "Pending_HOD_Review" || status == "V2_PENDING_HOD_REVIEW"))
            return true;

        if (userRoles.Contains("GM") && (status == "Pending_GM_Decision" || status == "V2_PENDING_GM_DECISION"))
            return true;

        if (userRoles.Contains("HR") &&
            (status == "Pending_HR_Processing" || status == "V2_PENDING_HR_PROMOTION" || status == "V2_PENDING_HR_LOW_PERFORMER"))
            return true;

        return false;
    }

    private async Task<RoleSpecificStatsDto?> GetRoleSpecificStatsAsync(int userId, List<string> userRoles, CancellationToken cancellationToken)
    {
        var primaryRole = userRoles.FirstOrDefault(r => r != "Employee");
        if (string.IsNullOrEmpty(primaryRole)) return null;
        
        var stats = new RoleSpecificStatsDto { Role = primaryRole };
        
        switch (primaryRole)
        {
            case "RM":
                var rmEmployeeIds = await _context.UserManagerMappings
                    .Where(m => m.ManagerUserId == userId)
                    .Select(m => m.EmployeeUserId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                stats.DirectReports = rmEmployeeIds.Count;

                stats.GoalSetsAwaitingReview = await _context.Set<Evaluation>()
                    .Include(e => e.Reviews)
                    .Where(e =>
                        (
                            ((e.ReportingManagerId == userId || rmEmployeeIds.Contains(e.EmployeeId)) &&
                             (e.Status == "Pending_RM_Review" ||
                              e.Status == "Pending_RM_Review_PostCompletion")) ||
                            (e.Status == "V2_PENDING_PARALLEL_REVIEWS" &&
                             e.Reviews.Any(r =>
                                 r.ReviewerRole == ReviewerRole.RM &&
                                 r.ReviewerUserId == userId &&
                                 r.Status == "Pending")) ||
                            (e.ReportingManagerId == userId &&
                             (e.Status == "V2_PENDING_RM_ACTIVATION_REVIEW" ||
                              e.Status == "V2_PENDING_TL_ACTIVATION_REVIEW"))
                        ))
                    .CountAsync(cancellationToken);
                
                var weekAgo = DateTime.UtcNow.AddDays(-7);
                stats.GoalSetsApprovedThisWeek = await _context.Set<ApprovalHistory>()
                    .Where(ah => ah.ActorUserId == userId && ah.ActorRole == "RM" && ah.Action.Contains("Approved") && ah.CreatedAt >= weekAgo)
                    .CountAsync(cancellationToken);
                break;
            
            case "TL":
                stats.TeamMembersCount = await _context.Set<Evaluation>()
                    .Where(e => e.TeamLeadId == userId)
                    .Select(e => e.EmployeeId)
                    .Distinct()
                    .CountAsync(cancellationToken);
                
                stats.PeerAssignmentsPending = await _context.Set<Evaluation>()
                    .Where(e => e.TeamLeadId == userId && e.Status == "Pending_Peer_Assignment")
                    .CountAsync(cancellationToken);
                
                stats.EvaluationsReadyForPeers = await _context.Set<Evaluation>()
                    .Where(e => e.TeamLeadId == userId && e.Status == "Pending_TL_Review")
                    .CountAsync(cancellationToken);
                break;
            
            case "HOD":
                var hodDeptIdsForStats = await _context.DepartmentHodMappings
                    .Where(m => m.HodUserId == userId)
                    .Select(m => m.DeptId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var hodPendingQuery = _context.Set<Evaluation>()
                    .Include(e => e.Employee)
                    .Where(e => e.Status == "Pending_HOD_Review" || e.Status == "V2_PENDING_HOD_REVIEW")
                    .AsQueryable();

                if (hodDeptIdsForStats.Count > 0)
                {
                    hodPendingQuery = hodPendingQuery.Where(e => hodDeptIdsForStats.Contains(e.Employee.DeptId));
                }

                stats.PromotionRecommendationsPending = await hodPendingQuery.CountAsync(cancellationToken);

                var deptEvaluationsQuery = _context.Set<Evaluation>()
                    .Include(e => e.Employee)
                    .Where(e => e.OverallScore.HasValue && e.OverallScore.Value > 0)
                    .AsQueryable();

                if (hodDeptIdsForStats.Count > 0)
                {
                    deptEvaluationsQuery = deptEvaluationsQuery.Where(e => hodDeptIdsForStats.Contains(e.Employee.DeptId));
                }

                var deptEvaluations = await deptEvaluationsQuery.ToListAsync(cancellationToken);

                stats.DepartmentSize = deptEvaluations.Select(e => e.EmployeeId).Distinct().Count();
                stats.DepartmentAverageScore = deptEvaluations.Any()
                    ? Math.Round(deptEvaluations.Average(e => e.OverallScore!.Value), 2)
                    : 0;
                break;
            
            case "GM":
                stats.TotalEmployees = await _context.Users.CountAsync(cancellationToken);
                stats.PendingPromotionDecisions = await _context.Set<Evaluation>()
                    .Where(e => e.Status == "Pending_GM_Decision" || e.Status == "V2_PENDING_GM_DECISION")
                    .CountAsync(cancellationToken);
                
                var quarterStart = new DateTime(DateTime.UtcNow.Year, ((DateTime.UtcNow.Month - 1) / 3) * 3 + 1, 1);
                stats.PromotionsApprovedThisQuarter = await _context.Set<ApprovalHistory>()
                    .Where(ah => ah.ActorRole == "GM" && ah.Action == "GmApprovedPromotion" && ah.CreatedAt >= quarterStart)
                    .CountAsync(cancellationToken);
                break;
        }
        
        return stats;
    }
}
