using Epecps.Domain.Enums;

namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for a set of personal goals created together (grouped view)
/// </summary>
public class PersonalGoalSetDto
{
    /// <summary>
    /// The ID that groups these goals together
    /// </summary>
    public Guid GoalSetId { get; set; }
    
    /// <summary>
    /// Template name used for this goal set
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of goals in this set
    /// </summary>
    public int GoalCount { get; set; }
    
    /// <summary>
    /// Total target score across all goals in the set
    /// </summary>
    public decimal TotalTargetScore { get; set; }
    
    /// <summary>
    /// Total current score across all goals in the set
    /// </summary>
    public decimal TotalCurrentScore { get; set; }
    
    /// <summary>
    /// Overall progress percentage (0-100) for the entire goal set
    /// Calculated as (TotalCurrentScore / TotalTargetScore) * 100
    /// </summary>
    public decimal ProgressPercent { get; set; }
    
    /// <summary>
    /// Indicates if this goal set is fully completed and can be submitted for evaluation
    /// True when all goals are completed (progress = 100%)
    /// </summary>
    public bool CanSubmitForEvaluation { get; set; }
    
    /// <summary>
    /// Start date (same for all goals in the set)
    /// </summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// Due date (same for all goals in the set)
    /// </summary>
    public DateTime DueDate { get; set; }
    
    /// <summary>
    /// Overall status of the goal set (based on individual goal statuses)
    /// </summary>
    public PersonalGoalStatus Status { get; set; }
    
    /// <summary>
    /// When the goal set was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Individual goals in this set (for expanded view)
    /// </summary>
    public List<PersonalGoalListDto> Goals { get; set; } = new();
    
    /// <summary>
    /// Categories covered by this goal set
    /// </summary>
    public List<string> Categories { get; set; } = new();
    
    /// <summary>
    /// Evaluation information if this goal set has been submitted
    /// </summary>
    public GoalSetEvaluationInfoDto? EvaluationInfo { get; set; }
}

/// <summary>
/// Information about the evaluation for a goal set
/// </summary>
public class GoalSetEvaluationInfoDto
{
    public int EvaluationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? OverallScore { get; set; }
    public DateTime SubmittedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public List<GoalSetApprovalStepDto> ApprovalSteps { get; set; } = new();
    public List<GoalSetApprovalHistoryEventDto> ApprovalHistory { get; set; } = new();
}

/// <summary>
/// Approval step for horizontal timeline display
/// </summary>
public class GoalSetApprovalStepDto
{
    public string Role { get; set; } = string.Empty; // Employee, RM, TL, Peer, HOD, GM
    public string ActorName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Submitted, Approved, Rejected, Pending
    public string? Comment { get; set; }
    public DateTime? ActionDate { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsPending { get; set; }
    public bool IsRejected { get; set; }
}

/// <summary>
/// Full chronological approval history event for goal-set evaluation.
/// </summary>
public class GoalSetApprovalHistoryEventDto
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
