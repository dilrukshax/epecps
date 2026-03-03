namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// DTO for RM to assign a single goal to an employee
/// </summary>
public class AssignGoalItemDto
{
    /// <summary>
    /// The ScoreItem (goal) from the goal library
    /// </summary>
    public Guid GoalItemId { get; set; }

    /// <summary>
    /// Title for the goal (defaults to ScoreItem name if not provided)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// RM instructions or description for the employee
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Custom activities the RM wants the employee to perform
    /// </summary>
    public List<string> CustomActivities { get; set; } = new();
}

/// <summary>
/// DTO for RM to assign goals to an employee (batch)
/// </summary>
public class RmAssignGoalsDto
{
    /// <summary>
    /// The employee to assign goals to
    /// </summary>
    public int EmployeeUserId { get; set; }

    /// <summary>
    /// Goal period in months
    /// </summary>
    public int GoalPeriodMonths { get; set; } = 6;

    /// <summary>
    /// Start date of the goal period
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Due date for the goal period
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// List of goals to assign
    /// </summary>
    public List<AssignGoalItemDto> Goals { get; set; } = new();
}

/// <summary>
/// Response DTO after RM assigns goals
/// </summary>
public class RmAssignGoalsResponseDto
{
    public Guid GoalSetId { get; set; }
    public int GoalCount { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for listing goal assignments made by the RM
/// </summary>
public class GoalAssignmentListDto
{
    public Guid Id { get; set; }
    public Guid GoalSetId { get; set; }
    public int EmployeeUserId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
    public string GoalItemName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for listing employees that the RM can assign goals to
/// </summary>
public class RmEmployeeDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

/// <summary>
/// DTO for a goal available in the system library (flat view for RM)
/// </summary>
public class GoalLibraryItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public decimal TargetScore { get; set; }
    public decimal MaxScore { get; set; }
    public bool IsMandatory { get; set; }
}
