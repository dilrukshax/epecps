namespace Epecps.Domain.Entities;

public class ReviewItem
{
    public int ItemId { get; set; }
    public int ReviewId { get; set; }
    public int? GoalId { get; set; }
    public int? CompetencyId { get; set; }
    public decimal RatingValue { get; set; }
    public string? Comment { get; set; }

    // Navigation properties
    public Review Review { get; set; } = null!;
    public EmployeeGoal? Goal { get; set; }
    public Competency? Competency { get; set; }
}
