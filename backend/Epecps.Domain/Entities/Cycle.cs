namespace Epecps.Domain.Entities;

public class Cycle
{
    public int CycleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;

    // Navigation property removed - not needed since EvaluationConfiguration defines the relationship
}
