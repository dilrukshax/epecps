namespace Epecps.Domain.Entities;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int DeptId { get; set; }

    // Navigation properties
    public Department Department { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Evaluation> EvaluationsAsEmployee { get; set; } = new List<Evaluation>();
    public ICollection<Evaluation> EvaluationsAsReportingManager { get; set; } = new List<Evaluation>();
    public ICollection<Evaluation> EvaluationsAsTeamLead { get; set; } = new List<Evaluation>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<PeerAssignment> PeerAssignments { get; set; } = new List<PeerAssignment>();
    public ICollection<PromotionCase> PromotionCasesRecommended { get; set; } = new List<PromotionCase>();
    public ICollection<PromotionCase> PromotionCasesDecided { get; set; } = new List<PromotionCase>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
