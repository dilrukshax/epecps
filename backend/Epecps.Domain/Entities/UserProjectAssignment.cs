namespace Epecps.Domain.Entities;

public class UserProjectAssignment
{
    public int UserProjectAssignmentId { get; set; }
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public string AssignmentRole { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
