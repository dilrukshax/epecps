namespace Epecps.Domain.Enums;

/// <summary>
/// Status of an RM-assigned goal
/// </summary>
public enum AssignedGoalStatus
{
    /// <summary>
    /// Goal has been assigned by RM but not yet accepted by the employee
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Employee has accepted the assigned goal
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Employee has rejected/declined the assigned goal
    /// </summary>
    Rejected = 2
}
