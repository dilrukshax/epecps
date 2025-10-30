namespace Epecps.Domain.Entities;

public class PromotionCase
{
    public int PromotionCaseId { get; set; }
    public int EvaluationId { get; set; }
    public int? RecommendedByHodId { get; set; }
    public DateTime? RecommendedAt { get; set; }
    public PromotionDecision GmDecision { get; set; }
    public int? GmDecidedById { get; set; }
    public DateTime? GmDecidedAt { get; set; }
    public string? DecisionReason { get; set; }

    // Navigation properties
    public Evaluation Evaluation { get; set; } = null!;
    public User? RecommendedByHod { get; set; }
    public User? GmDecidedBy { get; set; }
}
