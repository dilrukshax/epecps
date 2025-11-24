using Epecps.Domain.Enums;

namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for detailed personal goal view
/// </summary>
public class PersonalGoalDetailDto
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public Guid GoalItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetScore { get; set; }
    public decimal CurrentScore { get; set; }

    /// <summary>
    /// Progress percentage (0-100) calculated as (CurrentScore / TargetScore) * 100
    /// </summary>
    public decimal ProgressPercent { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public PersonalGoalStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Framework metadata
    public string CategoryName { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string GoalItemName { get; set; } = string.Empty;
    public string? GoalItemDescription { get; set; }

    // Activities
    public List<PersonalGoalActivityDto> Activities { get; set; } = new();
}
