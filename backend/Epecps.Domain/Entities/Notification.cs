namespace Epecps.Domain.Entities;

public class Notification
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
