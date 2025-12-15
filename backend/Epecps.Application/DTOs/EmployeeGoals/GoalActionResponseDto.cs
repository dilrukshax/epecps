namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// Response DTO for goal start/complete actions
/// </summary>
public class GoalActionResponseDto
{
    /// <summary>
    /// The goal ID that was acted upon
    /// </summary>
    public Guid GoalId { get; set; }
    
    /// <summary>
    /// The new status of the goal after the action
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// A human-readable message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates whether the evaluation workflow has continued to the next stage
    /// (e.g., after all goals are completed, workflow moves to TL/Peer review)
    /// </summary>
    public bool WorkflowContinued { get; set; }
    
    /// <summary>
    /// The evaluation ID if workflow has continued
    /// </summary>
    public int? EvaluationId { get; set; }
    
    /// <summary>
    /// The new evaluation status if workflow has continued
    /// </summary>
    public string? EvaluationStatus { get; set; }
}
