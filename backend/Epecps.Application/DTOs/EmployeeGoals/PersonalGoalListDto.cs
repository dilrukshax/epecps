using Epecps.Domain.Enums;

namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for personal goal list item (summary view)
/// </summary>
public class PersonalGoalListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string GoalItemName { get; set; } = string.Empty;
    public decimal TargetScore { get; set; }
    public decimal CurrentScore { get; set; }
    public PersonalGoalStatus Status { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
