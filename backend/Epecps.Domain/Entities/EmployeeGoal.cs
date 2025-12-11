namespace Epecps.Domain.Entities;

public class EmployeeGoal
{
    public int GoalId { get; set; }
    public int EvaluationId { get; set; }
    public Guid? PersonalGoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal WeightPct { get; set; }
    public string? EvidenceUri { get; set; }

    // Navigation properties
    public Evaluation Evaluation { get; set; } = null!;
    public PersonalGoal? PersonalGoal { get; set; }
    public ICollection<ReviewItem> ReviewItems { get; set; } = new List<ReviewItem>();
}
