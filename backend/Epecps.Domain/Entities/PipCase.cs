namespace Epecps.Domain.Entities;

/// <summary>
/// HR owned low-performer action plan case.
/// </summary>
public class PipCase
{
    public int PipCaseId { get; set; }
    public int EvaluationId { get; set; }
    public int EmployeeUserId { get; set; }
    public int AssignedHrUserId { get; set; }
    public string Status { get; set; } = "Open"; // Open, InProgress, Closed
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public DateTime? ClosedAt { get; set; }

    public Evaluation Evaluation { get; set; } = null!;
    public User EmployeeUser { get; set; } = null!;
    public User AssignedHrUser { get; set; } = null!;
    public ICollection<PipActionItem> ActionItems { get; set; } = new List<PipActionItem>();
}

