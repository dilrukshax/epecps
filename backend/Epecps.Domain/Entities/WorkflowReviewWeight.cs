namespace Epecps.Domain.Entities;

/// <summary>
/// Configurable workflow-v2 review weights used for final score calculation.
/// </summary>
public class WorkflowReviewWeight
{
    public int WorkflowReviewWeightId { get; set; }
    public string ReviewerKey { get; set; } = string.Empty; // Self, TL, RM, Peer1, Peer2
    public decimal WeightPercent { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

