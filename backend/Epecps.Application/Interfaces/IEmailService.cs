namespace Epecps.Application.Interfaces;

/// <summary>
/// Service for sending email notifications
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send an email asynchronously
    /// </summary>
    Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue an email to be sent in the background
    /// </summary>
    Task QueueEmailAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send evaluation notification email
    /// </summary>
    Task SendEvaluationNotificationAsync(
        string recipientEmail,
        string recipientName,
        string employeeName,
        string action,
        string role,
        string? comment = null,
        int? evaluationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send approval notification email
    /// </summary>
    Task SendApprovalNotificationAsync(
        string recipientEmail,
        string recipientName,
        string employeeName,
        string approverName,
        string approverRole,
        string nextStep,
        int evaluationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send rejection notification email
    /// </summary>
    Task SendRejectionNotificationAsync(
        string recipientEmail,
        string recipientName,
        string employeeName,
        string rejectorName,
        string rejectorRole,
        string reason,
        int evaluationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send promotion notification email
    /// </summary>
    Task SendPromotionNotificationAsync(
        string recipientEmail,
        string recipientName,
        string employeeName,
        bool isApproved,
        string? comment = null,
        CancellationToken cancellationToken = default);
}
