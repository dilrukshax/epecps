namespace Epecps.Domain.Enums;

/// <summary>
/// Status of a personal goal
/// </summary>
public enum PersonalGoalStatus
{
    Draft = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
    UnderEvaluation = 4,
    /// <summary>
    /// Goal is pending RM review/approval
    /// </summary>
    PendingRMReview = 5,
    /// <summary>
    /// Goal has been approved by RM and is ready for employee to start
    /// </summary>
    ApprovedByRM = 6,
    /// <summary>
    /// Goal has been returned to employee by RM for revision
    /// </summary>
    ReturnedToEmployee = 7
}
