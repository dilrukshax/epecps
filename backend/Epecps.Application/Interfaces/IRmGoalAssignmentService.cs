using Epecps.Application.DTOs.EmployeeGoals;

namespace Epecps.Application.Interfaces;

/// <summary>
/// Service interface for RM goal assignment operations.
/// RM browses the goal library and assigns goals to employees.
/// </summary>
public interface IRmGoalAssignmentService
{
    /// <summary>
    /// Get all goals from the system goal library (all active ScoreItems across published templates).
    /// Displayed as a flat list with category/template info for RM to browse.
    /// </summary>
    Task<List<GoalLibraryItemDto>> GetGoalLibraryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get employees that the RM manages (same department or assigned as RM).
    /// </summary>
    Task<List<RmEmployeeDto>> GetMyEmployeesAsync(int rmUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// RM assigns a set of goals to an employee.
    /// Creates GoalAssignment records and corresponding PersonalGoal records for the employee.
    /// Also auto-submits the goal set for RM approval (auto-approved since RM created them).
    /// </summary>
    Task<RmAssignGoalsResponseDto> AssignGoalsToEmployeeAsync(int rmUserId, RmAssignGoalsDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all goal assignments made by this RM.
    /// </summary>
    Task<List<GoalAssignmentListDto>> GetMyAssignmentsAsync(int rmUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get goal assignments for a specific employee (from this RM).
    /// </summary>
    Task<List<GoalAssignmentListDto>> GetAssignmentsForEmployeeAsync(int rmUserId, int employeeUserId, CancellationToken cancellationToken = default);
}
