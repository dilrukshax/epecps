using Epecps.Application.DTOs.Evaluations;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Domain.Enums;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// Service implementation for handling review scoring during evaluation workflow
/// NOW WITH REVIEW HISTORY TRACKING
/// </summary>
public class ReviewScoringService : IReviewScoringService
{
    private readonly EpecpsDbContext _context;
    private readonly IEmailService _emailService;

    public ReviewScoringService(EpecpsDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<ReviewScoringResponseDto> SubmitRmReviewScoringAsync(
        int evaluationId,
        int reviewId,
        int rmUserId,
        SubmitRmReviewScoringDto dto,
        CancellationToken cancellationToken = default)
    {
        // Validate evaluation exists
        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.Reviews)
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
            throw new NotFoundException(nameof(Evaluation), evaluationId);

        // Validate review exists and belongs to this evaluation
        var review = await _context.Set<Review>()
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.EvaluationId == evaluationId, cancellationToken);

        if (review == null)
            throw new NotFoundException(nameof(Review), reviewId);

        // Validate this is an RM review
        if (review.ReviewerRole != ReviewerRole.RM)
            throw new BusinessRuleException("Only RM reviews can submit item-level scores.");

        // Validate RM is the correct reviewer
        if (review.ReviewerUserId != rmUserId)
            throw new BusinessRuleException("You are not the assigned reviewer for this evaluation.");

        // Validate at least one item score
        if (!dto.ItemScores.Any())
            throw new BusinessRuleException("At least one goal score must be provided.");

        // Get all personal goals in this evaluation's goal set
        var personalGoals = await _context.PersonalGoals
            .Where(pg => pg.GoalSetId == evaluation.GoalSetId && pg.UserId == evaluation.EmployeeId)
            .ToListAsync(cancellationToken);

        if (!personalGoals.Any())
            throw new NotFoundException("No personal goals found for this evaluation.");

        // Validate all submitted goal IDs exist in this goal set
        var submittedGoalIds = dto.ItemScores.Select(s => s.PersonalGoalId).ToHashSet();
        var invalidGoals = submittedGoalIds.Where(id => !personalGoals.Any(pg => pg.Id == id)).ToList();

        if (invalidGoals.Any())
            throw new BusinessRuleException($"One or more goal IDs are not part of this evaluation's goal set.");

        // ? Get existing scores for history tracking
        var existingScores = await _context.Set<ReviewScore>()
            .Where(rs => rs.ReviewId == reviewId)
            .ToListAsync(cancellationToken);

        // ? Create history entries for updates/deletions
        foreach (var existingScore in existingScores)
        {
            var goal = personalGoals.FirstOrDefault(pg => pg.Id == existingScore.PersonalGoalId);
            
            var historyEntry = new ReviewScoreHistory
            {
                ReviewId = reviewId,
                EvaluationId = evaluationId,
                ReviewerUserId = rmUserId,
                ReviewerRole = ReviewerRole.RM,
                PersonalGoalId = existingScore.PersonalGoalId,
                GoalTitle = goal?.Title,
                PreviousScore = existingScore.ScoreValue,
                NewScore = 0, // Placeholder for deleted
                PreviousComment = existingScore.Comment,
                NewComment = "Score replaced",
                Action = "Deleted",
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<ReviewScoreHistory>().Add(historyEntry);
        }

        _context.Set<ReviewScore>().RemoveRange(existingScores);

        // Create new ReviewScore records with history tracking
        decimal totalScore = 0;
        foreach (var itemScore in dto.ItemScores)
        {
            var goal = personalGoals.First(pg => pg.Id == itemScore.PersonalGoalId);
            var existingScore = existingScores.FirstOrDefault(es => es.PersonalGoalId == itemScore.PersonalGoalId);

            var reviewScore = new ReviewScore
            {
                EvaluationId = evaluationId,
                ReviewId = reviewId,
                ReviewerId = rmUserId,
                PersonalGoalId = itemScore.PersonalGoalId,
                ScoreValue = itemScore.ScoreValue,
                Comment = itemScore.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<ReviewScore>().Add(reviewScore);
            totalScore += itemScore.ScoreValue;

            // ? Create history entry for new/updated score
            var historyAction = existingScore == null ? "Created" : "Updated";
            var historyEntry = new ReviewScoreHistory
            {
                ReviewId = reviewId,
                EvaluationId = evaluationId,
                ReviewerUserId = rmUserId,
                ReviewerRole = ReviewerRole.RM,
                PersonalGoalId = itemScore.PersonalGoalId,
                GoalTitle = goal.Title,
                PreviousScore = existingScore?.ScoreValue,
                NewScore = itemScore.ScoreValue,
                PreviousComment = existingScore?.Comment,
                NewComment = itemScore.Comment,
                Action = historyAction,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<ReviewScoreHistory>().Add(historyEntry);
        }

        // Calculate average score
        decimal averageScore = totalScore / dto.ItemScores.Count;

        // Update review
        review.OverallScore = averageScore;
        review.OverallComment = dto.OverallComment ?? review.OverallComment;
        review.Status = "Completed";
        review.SubmittedAt = DateTime.UtcNow;

        // Create approval history
        var approvalHistory = new ApprovalHistory
        {
            EvaluationId = evaluationId,
            ReviewId = reviewId,
            ActorUserId = rmUserId,
            ActorRole = "RM",
            Action = "RmSubmittedScores",
            Comment = $"Submitted scores for {dto.ItemScores.Count} goal(s). Average score: {averageScore:F2}",
            FromStatus = evaluation.Status,
            ToStatus = evaluation.Status, // Status doesn't change on RM submission
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ApprovalHistory>().Add(approvalHistory);

        // Create audit log
        var auditLog = new AuditLog
        {
            ActorUserId = rmUserId,
            EntityType = "Review",
            EntityId = reviewId,
            Action = "RM_SCORES_SUBMITTED",
            BeforeJson = System.Text.Json.JsonSerializer.Serialize(new { ItemCount = 0 }),
            AfterJson = System.Text.Json.JsonSerializer.Serialize(new { ItemCount = dto.ItemScores.Count, AverageScore = averageScore }),
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<AuditLog>().Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        // Send confirmation notification
        var notification = new Notification
        {
            UserId = rmUserId,
            Subject = $"Review Scores Submitted: {evaluation.Employee?.FullName ?? "Employee"}",
            Channel = "Email",
            SentAt = DateTime.UtcNow
        };

        _context.Set<Notification>().Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        return new ReviewScoringResponseDto
        {
            ReviewId = reviewId,
            EvaluationId = evaluationId,
            Message = $"Successfully submitted scores for {dto.ItemScores.Count} goal(s).",
            CalculatedScore = averageScore,
            EvaluationStatus = evaluation.Status
        };
    }

    public async Task<ReviewScoringResponseDto> SubmitOverallReviewScoringAsync(
        int evaluationId,
        int reviewId,
        int reviewerUserId,
        SubmitOverallReviewScoringDto dto,
        CancellationToken cancellationToken = default)
    {
        // Validate evaluation exists
        var evaluation = await _context.Set<Evaluation>()
            .Include(e => e.Reviews)
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, cancellationToken);

        if (evaluation == null)
            throw new NotFoundException(nameof(Evaluation), evaluationId);

        // Validate review exists
        var review = await _context.Set<Review>()
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.EvaluationId == evaluationId, cancellationToken);

        if (review == null)
            throw new NotFoundException(nameof(Review), reviewId);

        // Validate this is NOT an RM review (RM submits item scores, not overall)
        if (review.ReviewerRole == ReviewerRole.RM)
            throw new BusinessRuleException("RM reviewers submit item-level scores, not overall scores.");

        // Validate this is the correct reviewer
        if (review.ReviewerUserId != reviewerUserId)
            throw new BusinessRuleException("You are not the assigned reviewer for this evaluation.");

        // ? Get existing overall score for history tracking
        var existingOverallScore = await _context.Set<ReviewScore>()
            .FirstOrDefaultAsync(rs => rs.ReviewId == reviewId && rs.PersonalGoalId == null, cancellationToken);

        // ? Create history entry if score already exists (update scenario)
        if (existingOverallScore != null)
        {
            var updateHistory = new ReviewScoreHistory
            {
                ReviewId = reviewId,
                EvaluationId = evaluationId,
                ReviewerUserId = reviewerUserId,
                ReviewerRole = review.ReviewerRole,
                PersonalGoalId = null, // Overall score
                GoalTitle = null,
                PreviousScore = existingOverallScore.ScoreValue,
                NewScore = dto.OverallScore,
                PreviousComment = existingOverallScore.Comment,
                NewComment = dto.Comment,
                Action = "Updated",
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<ReviewScoreHistory>().Add(updateHistory);
            _context.Set<ReviewScore>().Remove(existingOverallScore);
        }

        // Create ReviewScore record for overall score
        var reviewScore = new ReviewScore
        {
            EvaluationId = evaluationId,
            ReviewId = reviewId,
            ReviewerId = reviewerUserId,
            PersonalGoalId = null, // null indicates overall score
            ScoreValue = dto.OverallScore,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ReviewScore>().Add(reviewScore);

        // ? Create history entry for new score (create scenario)
        if (existingOverallScore == null)
        {
            var createHistory = new ReviewScoreHistory
            {
                ReviewId = reviewId,
                EvaluationId = evaluationId,
                ReviewerUserId = reviewerUserId,
                ReviewerRole = review.ReviewerRole,
                PersonalGoalId = null,
                GoalTitle = null,
                PreviousScore = null,
                NewScore = dto.OverallScore,
                PreviousComment = null,
                NewComment = dto.Comment,
                Action = "Created",
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<ReviewScoreHistory>().Add(createHistory);
        }

        // Update review
        review.OverallScore = dto.OverallScore;
        review.OverallComment = dto.Comment ?? review.OverallComment;
        review.Status = "Completed";
        review.SubmittedAt = DateTime.UtcNow;

        // Create approval history
        var approvalHistory = new ApprovalHistory
        {
            EvaluationId = evaluationId,
            ReviewId = reviewId,
            ActorUserId = reviewerUserId,
            ActorRole = review.ReviewerRole.ToString(),
            Action = $"{review.ReviewerRole}SubmittedScore",
            Comment = $"Submitted overall score: {dto.OverallScore}. {dto.Comment}",
            FromStatus = evaluation.Status,
            ToStatus = evaluation.Status,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ApprovalHistory>().Add(approvalHistory);

        // Create audit log
        var auditLog = new AuditLog
        {
            ActorUserId = reviewerUserId,
            EntityType = "Review",
            EntityId = reviewId,
            Action = $"{review.ReviewerRole.ToString().ToUpper()}_OVERALL_SCORE_SUBMITTED",
            BeforeJson = System.Text.Json.JsonSerializer.Serialize(new { Score = existingOverallScore?.ScoreValue }),
            AfterJson = System.Text.Json.JsonSerializer.Serialize(new { Score = dto.OverallScore, Comment = dto.Comment }),
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<AuditLog>().Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        return new ReviewScoringResponseDto
        {
            ReviewId = reviewId,
            EvaluationId = evaluationId,
            Message = $"Successfully submitted overall score: {dto.OverallScore}",
            CalculatedScore = dto.OverallScore,
            EvaluationStatus = evaluation.Status
        };
    }

    public async Task<List<ReviewScoreDto>> GetEvaluationScoresAsync(
        int evaluationId,
        CancellationToken cancellationToken = default)
    {
        var scores = await _context.Set<ReviewScore>()
            .Include(rs => rs.PersonalGoal)
            .Where(rs => rs.EvaluationId == evaluationId)
            .OrderBy(rs => rs.CreatedAt)
            .Select(rs => new ReviewScoreDto
            {
                Id = rs.Id,
                PersonalGoalId = rs.PersonalGoalId,
                GoalTitle = rs.PersonalGoal != null ? rs.PersonalGoal.Title : null,
                ScoreValue = rs.ScoreValue,
                Comment = rs.Comment,
                CreatedAt = rs.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return scores;
    }

    public async Task<Dictionary<Guid, decimal>> CalculateGoalAverageScoresAsync(
        int evaluationId,
        CancellationToken cancellationToken = default)
    {
        var itemScores = await _context.Set<ReviewScore>()
            .Where(rs => rs.EvaluationId == evaluationId && rs.PersonalGoalId != null)
            .GroupBy(rs => rs.PersonalGoalId)
            .Select(g => new
            {
                GoalId = g.Key,
                AverageScore = g.Average(rs => rs.ScoreValue)
            })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, decimal>();
        foreach (var item in itemScores)
        {
            if (item.GoalId.HasValue)
            {
                result[item.GoalId.Value] = Math.Round((decimal)item.AverageScore, 2);
            }
        }

        return result;
    }

    public async Task<decimal> CalculateOverallEvaluationScoreAsync(
        int evaluationId,
        CancellationToken cancellationToken = default)
    {
        var overallScores = await _context.Set<ReviewScore>()
            .Include(rs => rs.Review)
            .Where(rs => rs.EvaluationId == evaluationId 
                && rs.PersonalGoalId == null 
                && rs.Review.Status == "Completed")
            .Select(rs => rs.ScoreValue)
            .ToListAsync(cancellationToken);

        if (!overallScores.Any())
            return 0;

        var averageScore = Math.Round(overallScores.Average(), 2);

        return averageScore;
    }
}
