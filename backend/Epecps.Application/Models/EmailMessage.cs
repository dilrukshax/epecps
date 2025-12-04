namespace Epecps.Application.Models;

/// <summary>
/// Email message to be queued for background processing
/// </summary>
public class EmailMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ToEmail { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public int RetryCount { get; set; } = 0;
    public DateTime? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
    public EmailStatus Status { get; set; } = EmailStatus.Queued;
}

/// <summary>
/// Email sending status
/// </summary>
public enum EmailStatus
{
    Queued = 0,
    Sending = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4
}
