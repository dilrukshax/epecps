namespace Epecps.Domain.Entities;

public class TrainingRecommendation
{
    public int TrainingRecId { get; set; }
    public int EvaluationId { get; set; }
    public int TrainingMaterialId { get; set; }
    public string? Reason { get; set; }

    // Navigation properties
    public Evaluation Evaluation { get; set; } = null!;
    public TrainingMaterial TrainingMaterial { get; set; } = null!;
}
