using Epecps.Domain.Enums;

namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for personal goal list item (summary view)
/// </summary>
public class PersonalGoalListDto
{
    public Guid Id { get; set; }
    public Guid? GoalSetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string GoalItemName { get; set; } = string.Empty;
    public decimal TargetScore { get; set; }
    public decimal CurrentScore { get; set; }
    
    /// <summary>
    /// Progress percentage (0-100) calculated as (CurrentScore / TargetScore) * 100
    /// </summary>
    public decimal ProgressPercent { get; set; }
    
    public PersonalGoalStatus Status { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletionEvidenceUrl { get; set; }
    public string? CompletionCertificationUrl { get; set; }
    public string? CompletionSummary { get; set; }
    public string? CompletionComment { get; set; }
}
