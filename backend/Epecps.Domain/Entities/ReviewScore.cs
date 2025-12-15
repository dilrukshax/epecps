namespace Epecps.Domain.Entities;

/// <summary>
/// Stores scores assigned by reviewers during evaluation
/// Supports both item-level scores (RM reviews individual goals)
/// and overall scores (TL, HOD, GM provide single evaluation score)
/// </summary>
public class ReviewScore
{
    public int Id { get; set; }
    
    /// <summary>
    /// The evaluation this score belongs to
    /// </summary>
    public int EvaluationId { get; set; }
    
    /// <summary>
    /// The review this score was submitted with
    /// </summary>
    public int ReviewId { get; set; }
    
    /// <summary>
    /// The reviewer (RM, TL, HOD, GM, etc.)
    /// </summary>
    public int ReviewerId { get; set; }
    
    /// <summary>
    /// Optional: If null, this is an overall score. If set, this is an item-level score (RM stage)
    /// References the PersonalGoal that was scored
    /// </summary>
    public Guid? PersonalGoalId { get; set; }
    
    /// <summary>
    /// The score value (typically 1-10 for reviews, 0-100 for goals)
    /// </summary>
    public decimal ScoreValue { get; set; }
    
    /// <summary>
    /// Optional comment explaining the score
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// When this score was submitted
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// If updated/revised
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Evaluation Evaluation { get; set; } = null!;
    public Review Review { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
    public PersonalGoal? PersonalGoal { get; set; }
}
