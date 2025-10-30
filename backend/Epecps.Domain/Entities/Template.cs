namespace Epecps.Domain.Entities;

public class Template
{
    public int TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string RatingScaleJson { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsActive { get; set; }
}
