using Epecps.Domain.Entities;
using Epecps.Domain.Enums;

namespace Epecps.Application.DTOs.Evaluations;

/// <summary>
/// Detailed evaluation information including reviews, goals, and approval history
/// </summary>
public class EvaluationDetailDto
{
    public int EvaluationId { get; set; }
    public int CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
    public int ReportingManagerId { get; set; }
    public string ReportingManagerName { get; set; } = string.Empty;
    public int TeamLeadId { get; set; }
    public string TeamLeadName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? OverallScore { get; set; }
    
    public List<ReviewDto> Reviews { get; set; } = new List<ReviewDto>();
    public List<GoalDto> Goals { get; set; } = new List<GoalDto>();
    public List<ApprovalHistoryItemDto> ApprovalHistory { get; set; } = new List<ApprovalHistoryItemDto>();
    public List<PeerAssignmentDto> PeerAssignments { get; set; } = new List<PeerAssignmentDto>();
    public PromotionCaseDto? PromotionCase { get; set; }
}

public class ReviewDto
{
    public int ReviewId { get; set; }
    public int ReviewerUserId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public ReviewerRole ReviewerRole { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? OverallComment { get; set; }
    public decimal? OverallScore { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public List<ReviewItemDto> Items { get; set; } = new List<ReviewItemDto>();
    public List<ReviewScoreDto> Scores { get; set; } = new List<ReviewScoreDto>();
}

public class ReviewItemDto
{
    public int ItemId { get; set; }
    public int? GoalId { get; set; }
    public string? GoalTitle { get; set; }
    public int? CompetencyId { get; set; }
    public string? CompetencyName { get; set; }
    public decimal RatingValue { get; set; }
    public string? Comment { get; set; }
}

public class GoalDto
{
    public int GoalId { get; set; }
    public Guid? PersonalGoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal WeightPct { get; set; }
    public string? EvidenceUri { get; set; }
    
    // Additional details
    public decimal TargetScore { get; set; }
    public decimal CurrentScore { get; set; }
    public decimal ProgressPercent { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Framework metadata
    public string? CategoryName { get; set; }
    public string? ItemName { get; set; }
    public string? GoalItemName { get; set; }
    
    // Activities
    public List<GoalActivityDto> Activities { get; set; } = new List<GoalActivityDto>();

    // Per-goal reviewer scores (all reviewers who scored this specific goal)
    public List<GoalReviewerScoreDto> ReviewerScores { get; set; } = new List<GoalReviewerScoreDto>();

    /// <summary>
    /// Average review score across all reviewers for this goal (null if no scores)
    /// </summary>
    public decimal? AverageReviewScore { get; set; }
}

/// <summary>
/// DTO for goal activities in evaluation view
/// </summary>
public class GoalActivityDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsFromTemplate { get; set; }
    public ActivityStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? EvidenceNotes { get; set; }
}

public class ApprovalHistoryItemDto
{
    public int Id { get; set; }
    public int ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PeerAssignmentDto
{
    public int PeerAssignmentId { get; set; }
    public int PeerUserId { get; set; }
    public string PeerName { get; set; } = string.Empty;
}

public class PromotionCaseDto
{
    public int PromotionCaseId { get; set; }
    public int? RecommendedByHodId { get; set; }
    public string? RecommendedByHodName { get; set; }
    public DateTime? RecommendedAt { get; set; }
    public PromotionDecision GmDecision { get; set; }
    public int? GmDecidedById { get; set; }
    public string? GmDecidedByName { get; set; }
    public DateTime? GmDecidedAt { get; set; }
    public string? DecisionReason { get; set; }
}
