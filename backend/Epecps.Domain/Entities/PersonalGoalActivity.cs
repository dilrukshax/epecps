using Epecps.Domain.Enums;

namespace Epecps.Domain.Entities;

/// <summary>
/// Represents an activity associated with a personal goal
/// All activities are custom-defined by the employee
/// </summary>
public class PersonalGoalActivity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The personal goal this activity belongs to
    /// </summary>
    public Guid PersonalGoalId { get; set; }
    
    /// <summary>
    /// DEPRECATED: Optional reference to a suggested activity template (always null - feature removed)
    /// Kept for database compatibility
    /// </summary>
    public Guid? SuggestedActivityId { get; set; }
    
    /// <summary>
    /// Description of the activity (required; custom text from employee)
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// DEPRECATED: Indicates if this activity was created from a suggested template (always false - feature removed)
    /// Kept for database compatibility
    /// </summary>
    public bool IsFromTemplate { get; set; } = false;
    
    /// <summary>
    /// Current status of the activity
    /// </summary>
    public ActivityStatus Status { get; set; } = ActivityStatus.NotStarted;
    
    /// <summary>
    /// Optional due date for this specific activity
    /// </summary>
    public DateTime? DueDate { get; set; }
    
    /// <summary>
    /// Optional evidence URL (e.g., link to completed work, certificate, etc.)
    /// </summary>
    public string? EvidenceUrl { get; set; }
    
    /// <summary>
    /// Optional notes or evidence description
    /// </summary>
    public string? EvidenceNotes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public PersonalGoal PersonalGoal { get; set; } = null!;
}
