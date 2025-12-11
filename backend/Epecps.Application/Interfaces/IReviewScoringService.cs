using Epecps.Application.DTOs.Evaluations;

namespace Epecps.Application.Interfaces;

/// <summary>
/// Service interface for handling review scoring during evaluation workflow
/// </summary>
public interface IReviewScoringService
{
    /// <summary>
    /// RM submits item-level scores for all personal goals in the evaluation
    /// Calculates average score for each goal
    /// </summary>
    Task<ReviewScoringResponseDto> SubmitRmReviewScoringAsync(
        int evaluationId,
        int reviewId,
        int rmUserId,
        SubmitRmReviewScoringDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// TL/HOD/GM submits overall evaluation score and comment
    /// </summary>
    Task<ReviewScoringResponseDto> SubmitOverallReviewScoringAsync(
        int evaluationId,
        int reviewId,
        int reviewerUserId,
        SubmitOverallReviewScoringDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all scores submitted for an evaluation
    /// </summary>
    Task<List<ReviewScoreDto>> GetEvaluationScoresAsync(
        int evaluationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate average score for all goals in an evaluation (based on RM scores)
    /// </summary>
    Task<Dictionary<Guid, decimal>> CalculateGoalAverageScoresAsync(
        int evaluationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate overall evaluation score combining all reviewer scores
    /// </summary>
    Task<decimal> CalculateOverallEvaluationScoreAsync(
        int evaluationId,
        CancellationToken cancellationToken = default);
}
