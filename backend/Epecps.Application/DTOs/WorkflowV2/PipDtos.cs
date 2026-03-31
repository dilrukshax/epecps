namespace Epecps.Application.DTOs.WorkflowV2;

public class PipActionItemDto
{
    public int PipActionItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? TrainingMaterialId { get; set; }
    public string? ExternalTrainingLink { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class PipCaseDto
{
    public int PipCaseId { get; set; }
    public int EvaluationId { get; set; }
    public int EmployeeUserId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int AssignedHrUserId { get; set; }
    public string AssignedHrName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ClosedAt { get; set; }
    public List<PipActionItemDto> ActionItems { get; set; } = new();
}

