namespace Epecps.Domain.Entities;

public class Document
{
    public int DocumentId { get; set; }
    public int EvaluationId { get; set; }
    public DocumentType Type { get; set; }
    public string Uri { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Evaluation Evaluation { get; set; } = null!;
}
