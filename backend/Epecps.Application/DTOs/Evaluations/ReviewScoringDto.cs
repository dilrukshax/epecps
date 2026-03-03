using System.ComponentModel.DataAnnotations;

namespace Epecps.Application.DTOs.Evaluations;

/// <summary>
/// DTO for individual item score submitted by RM during review
/// RM scores each PersonalGoalItem from 1-10
/// </summary>
public class ReviewItemScoreDto
{
    /// <summary>
    /// The PersonalGoal being scored
    /// </summary>
    [Required]
    public Guid PersonalGoalId { get; set; }
    
    /// <summary>
    /// Score value (typically 1-10)
    /// </summary>
    [Required]
    [Range(1, 10)]
    public decimal ScoreValue { get; set; }
    
    /// <summary>
    /// Optional comment on this item
    /// </summary>
    [MaxLength(500)]
    public string? Comment { get; set; }
}

/// <summary>
/// Request DTO for RM to submit item-level scores for all goals
/// </summary>
public class SubmitRmReviewScoringDto
{
    /// <summary>
    /// All item scores (one per goal)
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<ReviewItemScoreDto> ItemScores { get; set; } = new();
    
    /// <summary>
    /// Overall comment on the review
    /// </summary>
    [MaxLength(2000)]
    public string? OverallComment { get; set; }
}

/// <summary>
/// Request DTO for TL/HOD/GM to submit overall evaluation score
/// </summary>
public class SubmitOverallReviewScoringDto
{
    /// <summary>
    /// Overall evaluation score (typically 1-10)
    /// </summary>
    [Required]
    [Range(1, 10)]
    public decimal OverallScore { get; set; }
    
    /// <summary>
    /// Comment explaining the score
    /// </summary>
    [MaxLength(2000)]
    public string? Comment { get; set; }
}

/// <summary>
/// Response DTO after score submission
/// </summary>
public class ReviewScoringResponseDto
{
    public int ReviewId { get; set; }
    public int EvaluationId { get; set; }
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Average score of all items (for RM review only)
    /// </summary>
    public decimal? CalculatedScore { get; set; }
    
    /// <summary>
    /// New evaluation status after scoring
    /// </summary>
    public string EvaluationStatus { get; set; } = string.Empty;
}

/// <summary>
/// DTO for viewing review scores (included in ReviewDto response)
/// </summary>
public class ReviewScoreDto
{
    public int Id { get; set; }
    public int EvaluationId { get; set; }
    public int ReviewId { get; set; }
    public int ReviewerId { get; set; }
    public Guid? PersonalGoalId { get; set; }
    public string? GoalTitle { get; set; }
    public decimal ScoreValue { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for any reviewer (TL, Peer, HOD) to submit per-goal scores
/// alongside an overall score. This enables individual goal scoring for all reviewer roles.
/// </summary>
public class SubmitReviewWithGoalScoresDto
{
    /// <summary>
    /// Per-goal scores (one per goal)
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<ReviewItemScoreDto> GoalScores { get; set; } = new();

    /// <summary>
    /// Overall evaluation score (1-10), computed as average if not provided
    /// </summary>
    [Range(1, 10)]
    public decimal? OverallScore { get; set; }

    /// <summary>
    /// Overall comment on the review
    /// </summary>
    [MaxLength(2000)]
    public string? OverallComment { get; set; }
}

/// <summary>
/// DTO representing a single reviewer's score for a specific goal (used in GoalDto response)
/// </summary>
public class GoalReviewerScoreDto
{
    public int ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string ReviewerRole { get; set; } = string.Empty;
    public decimal ScoreValue { get; set; }
    public string? Comment { get; set; }
    public DateTime? ScoredAt { get; set; }
}
