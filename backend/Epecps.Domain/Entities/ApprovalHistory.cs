namespace Epecps.Domain.Entities;

/// <summary>
/// Tracks all approval actions and state transitions for evaluations
/// </summary>
public class ApprovalHistory
{
    public int Id { get; set; }
    
    /// <summary>
    /// The evaluation this approval action relates to
    /// </summary>
    public int EvaluationId { get; set; }
    
    /// <summary>
    /// Optional reference to a specific review (for review-based approvals)
    /// </summary>
    public int? ReviewId { get; set; }
    
    /// <summary>
    /// User who performed the action
    /// </summary>
    public int ActorUserId { get; set; }
    
    /// <summary>
    /// Role of the actor at the time of action (Employee, RM, TL, Peer, HOD, GM, HR)
    /// </summary>
    public string ActorRole { get; set; } = string.Empty;
    
    /// <summary>
    /// Action performed (e.g., Submitted, Approved, Rejected, Returned, RecommendedForPromotion, GmApproved, GmRejected)
    /// </summary>
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional comment explaining the action
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// Evaluation status before the action
    /// </summary>
    public string FromStatus { get; set; } = string.Empty;
    
    /// <summary>
    /// Evaluation status after the action
    /// </summary>
    public string ToStatus { get; set; } = string.Empty;
    
    /// <summary>
    /// When this action was performed
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Evaluation Evaluation { get; set; } = null!;
    public Review? Review { get; set; }
    public User ActorUser { get; set; } = null!;
}
