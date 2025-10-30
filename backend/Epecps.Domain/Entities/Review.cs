namespace Epecps.Domain.Entities;

public class Review
{
    public int ReviewId { get; set; }
    public int EvaluationId { get; set; }
    public int ReviewerUserId { get; set; }
    public ReviewerRole ReviewerRole { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? OverallComment { get; set; }
    public DateTime? SubmittedAt { get; set; }

    // Navigation properties
    public Evaluation Evaluation { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
    public ICollection<ReviewItem> ReviewItems { get; set; } = new List<ReviewItem>();
}
