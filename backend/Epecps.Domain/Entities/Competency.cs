namespace Epecps.Domain.Entities;

public class Competency
{
    public int CompetencyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetLevel { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<ReviewItem> ReviewItems { get; set; } = new List<ReviewItem>();
}
