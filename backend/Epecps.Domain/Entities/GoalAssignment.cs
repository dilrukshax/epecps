using Epecps.Domain.Enums;

namespace Epecps.Domain.Entities;

/// <summary>
/// Represents a goal assigned by a Reporting Manager (RM) to an employee.
/// The RM selects goals from the system's goal library (ScoreItems) and assigns them.
/// </summary>
public class GoalAssignment
{
    public Guid Id { get; set; }

    /// <summary>
    /// The RM who assigned the goal
    /// </summary>
    public int AssignedByUserId { get; set; }

    /// <summary>
    /// The employee the goal is assigned to
    /// </summary>
    public int AssignedToUserId { get; set; }

    /// <summary>
    /// Reference to the goal item (ScoreItem) from the goal library
    /// </summary>
    public Guid GoalItemId { get; set; }

    /// <summary>
    /// Groups assignments made together (same session) into a set
    /// </summary>
    public Guid GoalSetId { get; set; }

    /// <summary>
    /// RM-defined title for this goal (defaults to ScoreItem name)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// RM-defined description or instructions
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Target score for this goal
    /// </summary>
    public decimal TargetScore { get; set; } = 100;

    /// <summary>
    /// Start date of the goal period
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Due date for completing the goal
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Status of this assignment
    /// </summary>
    public AssignedGoalStatus Status { get; set; } = AssignedGoalStatus.Pending;

    /// <summary>
    /// ID of the PersonalGoal created when the assignment is accepted/processed
    /// </summary>
    public Guid? PersonalGoalId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User AssignedByUser { get; set; } = null!;
    public User AssignedToUser { get; set; } = null!;
    public ScoreItem GoalItem { get; set; } = null!;
    public PersonalGoal? PersonalGoal { get; set; }
}
