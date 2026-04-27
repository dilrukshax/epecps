namespace Epecps.Application.DTOs.EmployeeGoals;

/// <summary>
/// Request DTO for completing a goal
/// Requires the employee to provide a completion comment and optionally provide supporting details
/// </summary>
public class CompleteGoalRequestDto
{
    /// <summary>
    /// Optional evidence URL or link to supporting documentation
    /// </summary>
    public string? EvidenceUrl { get; set; }

    /// <summary>
    /// Optional certification URL or link to relevant certificate/proof
    /// </summary>
    public string? CertificationUrl { get; set; }

    /// <summary>
    /// Optional summary of what was achieved for the goal
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// Required comment explaining how the goal was completed
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// Optional: The current score to set. If not provided, defaults to TargetScore (100%)
    /// Must be less than or equal to TargetScore
    /// </summary>
    public decimal? CurrentScore { get; set; }
}
