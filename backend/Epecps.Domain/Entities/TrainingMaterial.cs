namespace Epecps.Domain.Entities;

public class TrainingMaterial
{
    public int TrainingMaterialId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string? Tags { get; set; }

    // Navigation properties
    public ICollection<TrainingRecommendation> TrainingRecommendations { get; set; } = new List<TrainingRecommendation>();
}
