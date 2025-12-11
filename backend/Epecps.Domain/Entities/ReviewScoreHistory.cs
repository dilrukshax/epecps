namespace Epecps.Domain.Entities;

/// <summary>
/// Tracks the history of review score changes for audit purposes
/// Records every time a reviewer submits or updates their scores
/// </summary>
public class ReviewScoreHistory
{
    public int Id { get; set; }
    
    /// <summary>
    /// The review this history entry belongs to
    /// </summary>
    public int ReviewId { get; set; }
    
    /// <summary>
    /// The evaluation this score history is for
    /// </summary>
    public int EvaluationId { get; set; }
    
    /// <summary>
    /// The user who submitted/changed the score
    /// </summary>
    public int ReviewerUserId { get; set; }
    
    /// <summary>
    /// The reviewer role (RM, TL, Peer, etc.)
    /// </summary>
    public ReviewerRole ReviewerRole { get; set; }
    
    /// <summary>
    /// Optional: PersonalGoalId if this is an item-level score (RM only)
    /// NULL for overall scores (TL, Peer, HOD, GM)
    /// </summary>
    public Guid? PersonalGoalId { get; set; }
    
    /// <summary>
    /// The goal title at the time of scoring (for display)
    /// </summary>
    public string? GoalTitle { get; set; }
    
    /// <summary>
    /// Previous score value (NULL if first submission)
    /// </summary>
    public decimal? PreviousScore { get; set; }
    
    /// <summary>
    /// New score value
    /// </summary>
    public decimal NewScore { get; set; }
    
    /// <summary>
    /// Previous comment (NULL if first submission)
    /// </summary>
    public string? PreviousComment { get; set; }
    
    /// <summary>
    /// New comment
    /// </summary>
    public string? NewComment { get; set; }
    
    /// <summary>
    /// Action type: "Created", "Updated", "Deleted"
    /// </summary>
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// When this change was made
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Review Review { get; set; } = null!;
    public Evaluation Evaluation { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
    public PersonalGoal? PersonalGoal { get; set; }
}
