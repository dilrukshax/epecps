namespace Epecps.Domain.Entities;

/// <summary>
/// Individual tracked action item inside a PIP case.
/// </summary>
public class PipActionItem
{
    public int PipActionItemId { get; set; }
    public int PipCaseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? TrainingMaterialId { get; set; }
    public string? ExternalTrainingLink { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Done
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public PipCase PipCase { get; set; } = null!;
    public TrainingMaterial? TrainingMaterial { get; set; }
}

