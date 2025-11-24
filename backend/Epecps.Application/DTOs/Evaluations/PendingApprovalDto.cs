namespace Epecps.Application.DTOs.Evaluations;

/// <summary>
/// Represents a pending approval that requires action from the current user
/// </summary>
public class PendingApprovalDto
{
    public int EvaluationId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RequiredRole { get; set; } = string.Empty;
    public DateTime? SubmittedDate { get; set; }
    public int CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;
}
