namespace Epecps.Domain.Entities;

public class Evaluation
{
    public int EvaluationId { get; set; }
    public int CycleId { get; set; }
    public int EmployeeId { get; set; }
    public int ReportingManagerId { get; set; }
    public int TeamLeadId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? OverallScore { get; set; }
    public int? PreviousEvaluationId { get; set; }

    // Navigation properties
    public Cycle Cycle { get; set; } = null!;
    public User Employee { get; set; } = null!;
    public User ReportingManager { get; set; } = null!;
    public User TeamLead { get; set; } = null!;
    public Evaluation? PreviousEvaluation { get; set; }
    public ICollection<Evaluation> NextEvaluations { get; set; } = new List<Evaluation>();
    public ICollection<EmployeeGoal> EmployeeGoals { get; set; } = new List<EmployeeGoal>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<PeerAssignment> PeerAssignments { get; set; } = new List<PeerAssignment>();
    public ICollection<PromotionCase> PromotionCases { get; set; } = new List<PromotionCase>();
    public ICollection<TrainingRecommendation> TrainingRecommendations { get; set; } = new List<TrainingRecommendation>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
