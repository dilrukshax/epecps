namespace Epecps.Domain.Entities;

public class PeerAssignment
{
    public int PeerAssignmentId { get; set; }
    public int EvaluationId { get; set; }
    public int PeerUserId { get; set; }

    // Navigation properties
    public Evaluation Evaluation { get; set; } = null!;
    public User PeerUser { get; set; } = null!;
}
