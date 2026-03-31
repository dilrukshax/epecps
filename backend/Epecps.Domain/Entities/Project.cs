namespace Epecps.Domain.Entities;

public class Project
{
    public int ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public int? ProjectManagerUserId { get; set; }
    public int? SupervisorUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User? ProjectManagerUser { get; set; }
    public User? SupervisorUser { get; set; }
    public ICollection<UserProjectAssignment> UserProjectAssignments { get; set; } = new List<UserProjectAssignment>();
}
