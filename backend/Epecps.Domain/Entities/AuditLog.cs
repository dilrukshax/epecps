namespace Epecps.Domain.Entities;

public class AuditLog
{
    public int AuditId { get; set; }
    public int ActorUserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User ActorUser { get; set; } = null!;
}
