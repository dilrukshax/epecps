using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Text;
using Epecps.Application.Interfaces;
using Epecps.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// SMTP-based email service with background processing and retry logic
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly ConcurrentQueue<EmailMessage> _emailQueue;
    private static readonly string TemplateBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates");

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _emailQueue = new ConcurrentQueue<EmailMessage>();
    }

    /// <summary>
    /// Send email immediately (synchronous)
    /// </summary>
    public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Attempted to send email with empty recipient address");
                return;
            }

            if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
            {
                _logger.LogWarning("EmailSettings.SenderEmail is not configured. Email sending is disabled.");
                return;
            }

            using var client = CreateSmtpClient();
            using var mailMessage = CreateMailMessage(toEmail, toName, subject, htmlBody);

            _logger.LogInformation("Sending email to {Email} with subject: {Subject}", toEmail, subject);
            await client.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}: {Error}", toEmail, ex.Message);
            // Don't rethrow - allow the workflow to continue even if email fails
            // In production, you might want to queue for retry or alert admins
        }
    }

    /// <summary>
    /// Queue email for background processing
    /// </summary>
    public Task QueueEmailAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var emailMessage = new EmailMessage
        {
            ToEmail = toEmail,
            ToName = toName,
            Subject = subject,
            HtmlBody = htmlBody
        };

        _emailQueue.Enqueue(emailMessage);
        _logger.LogInformation("Email queued for {Email} with subject: {Subject}", toEmail, subject);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Send evaluation notification email
    /// </summary>
    public async Task SendEvaluationNotificationAsync(
        string recipientEmail,
        string recipientName,
        string employeeName,
        string action,
        string role,
        string? comment = null,
        int? evaluationId = null,
        CancellationToken cancellationToken = default)
    {
        var subject = $"EPECPS: {action} - {employeeName}";
        
        var htmlBody = GenerateEvaluationNotificationHtml(
            recipientName,
            employeeName,
            action,
            role,
            comment,
            evaluationId);

        // ? TEMPORARY FIX: Send immediately instead of queueing to bypass background service issue
        await SendEmailAsync(recipientEmail, recipientName, subject, htmlBody, cancellationToken);
        
        _logger.LogInformation("Evaluation notification email sent to {Email} for {Employee}", recipientEmail, employeeName);
    }

    /// <summary>
    /// Send approval notification email
    /// </summary>
    public async Task SendApprovalNotificationAsync(
        string recipientEmail,
        string recipientName,
        string employeeName,
        string approverName,
        string approverRole,
        string nextStep,
        int evaluationId,
        CancellationToken cancellationToken = default)
    {
        var subject = $"EPECPS: Evaluation Approved by {approverRole} - {employeeName}";
        
        var htmlBody = GenerateApprovalNotificationHtml(
            recipientName,
            employeeName,
            approverName,
            approverRole,
            nextStep,
            evaluationId);

        // ? TEMPORARY FIX: Send immediately
        await SendEmailAsync(recipientEmail, recipientName, subject, htmlBody, cancellationToken);
        
        _logger.LogInformation("Approval notification email sent to {Email} for {Employee}", recipientEmail, employeeName);
    }

    /// <summary>
    /// Send rejection notification
    /// </summary>
    public async Task SendRejectionNotificationAsync(
        string recipientEmail,
        string recipientName,
        string employeeName,
        string rejectorName,
        string rejectorRole,
        string reason,
        int evaluationId,
        CancellationToken cancellationToken = default)
    {
        var subject = $"EPECPS: Evaluation Rejected by {rejectorRole} - {employeeName}";
        
        var htmlBody = GenerateRejectionNotificationHtml(
            recipientName,
            employeeName,
            rejectorName,
            rejectorRole,
            reason,
            evaluationId);

        // ? TEMPORARY FIX: Send immediately
        await SendEmailAsync(recipientEmail, recipientName, subject, htmlBody, cancellationToken);
        
        _logger.LogInformation("Rejection notification email sent to {Email} for {Employee}", recipientEmail, employeeName);
    }

    /// <summary>
    /// Send promotion notification
    /// </summary>
    public async Task SendPromotionNotificationAsync(
        string recipientEmail,
        string recipientName,
        string employeeName,
        bool isApproved,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var subject = isApproved 
            ? $"EPECPS: Congratulations! Promotion Approved - {employeeName}"
            : $"EPECPS: Promotion Status Update - {employeeName}";
        
        var htmlBody = GeneratePromotionNotificationHtml(
            recipientName,
            employeeName,
            isApproved,
            comment);

        // ? TEMPORARY FIX: Send immediately
        await SendEmailAsync(recipientEmail, recipientName, subject, htmlBody, cancellationToken);
        
        _logger.LogInformation("Promotion notification email sent to {Email} for {Employee}", recipientEmail, employeeName);
    }

    #region Internal Methods

    /// <summary>
    /// Create SMTP client with configuration
    /// </summary>
    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrEmpty(_settings.Username) && !string.IsNullOrEmpty(_settings.Password))
        {
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        }

        return client;
    }

    /// <summary>
    /// Create mail message
    /// </summary>
    private MailMessage CreateMailMessage(string toEmail, string toName, string subject, string htmlBody)
    {
        // Validate email settings
        if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
        {
            throw new InvalidOperationException(
                "EmailSettings.SenderEmail is not configured. Please set the SenderEmail in appsettings.json under EmailSettings section.");
        }

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("Recipient email address cannot be empty.", nameof(toEmail));
        }

        var message = new MailMessage
        {
            From = new MailAddress(_settings.SenderEmail, _settings.SenderName ?? "EPECPS"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };

        message.To.Add(new MailAddress(toEmail, toName ?? "Recipient"));

        return message;
    }

    /// <summary>
    /// Generate HTML for evaluation notification
    /// </summary>
    private string GenerateEvaluationNotificationHtml(
        string recipientName,
        string employeeName,
        string action,
        string role,
        string? comment,
        int? evaluationId)
    {
        var actionUrl = evaluationId.HasValue 
            ? $"{_settings.BaseUrl}/evaluations/detail/{evaluationId}" 
            : $"{_settings.BaseUrl}/evaluations/my-approvals";

        var messageTitle = GetMessageTitle(action);
        var messageContent = GetMessageContent(action, employeeName, role);
        var actionBadgeClass = GetActionBadgeClass(action);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f4f4f4; }}
        .email-container {{ background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); overflow: hidden; }}
        .email-header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px; }}
        .greeting {{ font-size: 16px; margin-bottom: 20px; }}
        .message-box {{ background-color: #f8f9fa; border-left: 4px solid #667eea; padding: 15px 20px; margin: 20px 0; border-radius: 4px; }}
        .message-box h2 {{ margin-top: 0; color: #667eea; font-size: 18px; }}
        .info-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        .info-table td {{ padding: 10px; border-bottom: 1px solid #e9ecef; }}
        .info-table td:first-child {{ font-weight: 600; color: #666; width: 140px; }}
        .action-button {{ display: inline-block; padding: 12px 30px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white !important; text-decoration: none; border-radius: 6px; font-weight: 600; text-align: center; margin: 20px 0; }}
        .footer {{ background-color: #f8f9fa; padding: 20px 30px; text-align: center; color: #666; font-size: 14px; border-top: 1px solid #e9ecef; }}
        .badge {{ display: inline-block; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600; text-transform: uppercase; }}
        .badge-success {{ background-color: #d4edda; color: #155724; }}
        .badge-warning {{ background-color: #fff3cd; color: #856404; }}
        .badge-info {{ background-color: #d1ecf1; color: #0c5460; }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""email-header"">
            <h1>?? EPECPS - Employee Performance Evaluation</h1>
        </div>
        
        <div class=""email-body"">
            <p class=""greeting"">Hello <strong>{recipientName}</strong>,</p>
            
            <div class=""message-box"">
                <h2>{messageTitle}</h2>
                <p>{messageContent}</p>
            </div>
            
            <table class=""info-table"">
                <tr>
                    <td>Employee:</td>
                    <td><strong>{employeeName}</strong></td>
                </tr>
                <tr>
                    <td>Action:</td>
                    <td><span class=""badge {actionBadgeClass}"">{action}</span></td>
                </tr>
                <tr>
                    <td>Your Role:</td>
                    <td>{role}</td>
                </tr>
                {(evaluationId.HasValue ? $@"
                <tr>
                    <td>Evaluation ID:</td>
                    <td>#{evaluationId}</td>
                </tr>" : "")}
                {(!string.IsNullOrEmpty(comment) ? $@"
                <tr>
                    <td>Comment:</td>
                    <td><em>{comment}</em></td>
                </tr>" : "")}
            </table>
            
            <div style=""text-align: center;"">
                <a href=""{actionUrl}"" class=""action-button"">
                    View Evaluation Details
                </a>
            </div>
            
            <p style=""margin-top: 30px; color: #666; font-size: 14px;"">
                Please log in to the EPECPS system to review and take action on this evaluation.
            </p>
        </div>
        
        <div class=""footer"">
            <p><strong>Employee Performance Evaluation and Competency Progression System</strong></p>
            <p>This is an automated email. Please do not reply to this message.</p>
            <p>If you have questions, please contact your HR department.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generate HTML for approval notification
    /// </summary>
    private string GenerateApprovalNotificationHtml(
        string recipientName,
        string employeeName,
        string approverName,
        string approverRole,
        string nextStep,
        int evaluationId)
    {
        var actionUrl = $"{_settings.BaseUrl}/evaluations/detail/{evaluationId}";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f4f4f4; }}
        .email-container {{ background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); overflow: hidden; }}
        .email-header {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 30px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px; }}
        .greeting {{ font-size: 16px; margin-bottom: 20px; }}
        .message-box {{ background-color: #d1fae5; border-left: 4px solid #10b981; padding: 15px 20px; margin: 20px 0; border-radius: 4px; }}
        .message-box h2 {{ margin-top: 0; color: #059669; font-size: 18px; }}
        .info-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        .info-table td {{ padding: 10px; border-bottom: 1px solid #e9ecef; }}
        .info-table td:first-child {{ font-weight: 600; color: #666; width: 140px; }}
        .action-button {{ display: inline-block; padding: 12px 30px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white !important; text-decoration: none; border-radius: 6px; font-weight: 600; text-align: center; margin: 20px 0; }}
        .footer {{ background-color: #f8f9fa; padding: 20px 30px; text-align: center; color: #666; font-size: 14px; border-top: 1px solid #e9ecef; }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""email-header"">
            <h1>? Evaluation Approved</h1>
        </div>
        
        <div class=""email-body"">
            <p class=""greeting"">Hello <strong>{recipientName}</strong>,</p>
            
            <div class=""message-box"">
                <h2>Good News!</h2>
                <p>An evaluation has been approved and is now awaiting your action.</p>
            </div>
            
            <table class=""info-table"">
                <tr>
                    <td>Employee:</td>
                    <td><strong>{employeeName}</strong></td>
                </tr>
                <tr>
                    <td>Approved By:</td>
                    <td>{approverName} ({approverRole})</td>
                </tr>
                <tr>
                    <td>Evaluation ID:</td>
                    <td>#{evaluationId}</td>
                </tr>
                <tr>
                    <td>Next Step:</td>
                    <td><strong>{nextStep}</strong></td>
                </tr>
            </table>
            
            <div style=""text-align: center;"">
                <a href=""{actionUrl}"" class=""action-button"">
                    View Evaluation & Take Action
                </a>
            </div>
            
            <p style=""margin-top: 30px; color: #666; font-size: 14px;"">
                Please review the evaluation and complete your part of the approval process.
            </p>
        </div>
        
        <div class=""footer"">
            <p><strong>Employee Performance Evaluation and Competency Progression System</strong></p>
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generate HTML for rejection notification
    /// </summary>
    private string GenerateRejectionNotificationHtml(
        string recipientName,
        string employeeName,
        string rejectorName,
        string rejectorRole,
        string reason,
        int evaluationId)
    {
        var actionUrl = $"{_settings.BaseUrl}/evaluations/detail/{evaluationId}";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f4f4f4; }}
        .email-container {{ background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); overflow: hidden; }}
        .email-header {{ background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); color: white; padding: 30px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px; }}
        .greeting {{ font-size: 16px; margin-bottom: 20px; }}
        .message-box {{ background-color: #fee2e2; border-left: 4px solid #ef4444; padding: 15px 20px; margin: 20px 0; border-radius: 4px; }}
        .message-box h2 {{ margin-top: 0; color: #dc2626; font-size: 18px; }}
        .info-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        .info-table td {{ padding: 10px; border-bottom: 1px solid #e9ecef; }}
        .info-table td:first-child {{ font-weight: 600; color: #666; width: 140px; }}
        .reason-box {{ background-color: #fef3c7; border: 1px solid #fbbf24; padding: 15px; border-radius: 4px; margin: 15px 0; }}
        .action-button {{ display: inline-block; padding: 12px 30px; background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); color: white !important; text-decoration: none; border-radius: 6px; font-weight: 600; text-align: center; margin: 20px 0; }}
        .footer {{ background-color: #f8f9fa; padding: 20px 30px; text-align: center; color: #666; font-size: 14px; border-top: 1px solid #e9ecef; }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""email-header"">
            <h1>?? Evaluation Requires Revision</h1>
        </div>
        
        <div class=""email-body"">
            <p class=""greeting"">Hello <strong>{recipientName}</strong>,</p>
            
            <div class=""message-box"">
                <h2>Evaluation Rejected</h2>
                <p>An evaluation has been rejected and requires your attention.</p>
            </div>
            
            <table class=""info-table"">
                <tr>
                    <td>Employee:</td>
                    <td><strong>{employeeName}</strong></td>
                </tr>
                <tr>
                    <td>Rejected By:</td>
                    <td>{rejectorName} ({rejectorRole})</td>
                </tr>
                <tr>
                    <td>Evaluation ID:</td>
                    <td>#{evaluationId}</td>
                </tr>
            </table>

            <div class=""reason-box"">
                <p style=""margin: 0; font-weight: 600; color: #92400e;"">Reason for Rejection:</p>
                <p style=""margin: 10px 0 0 0; color: #451a03;"">{reason}</p>
            </div>
            
            <div style=""text-align: center;"">
                <a href=""{actionUrl}"" class=""action-button"">
                    View Evaluation Details
                </a>
            </div>
            
            <p style=""margin-top: 30px; color: #666; font-size: 14px;"">
                Please review the feedback and make necessary revisions to the evaluation.
            </p>
        </div>
        
        <div class=""footer"">
            <p><strong>Employee Performance Evaluation and Competency Progression System</strong></p>
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generate HTML for promotion notification
    /// </summary>
    private string GeneratePromotionNotificationHtml(
        string recipientName,
        string employeeName,
        bool isApproved,
        string? comment)
    {
        var actionUrl = $"{_settings.BaseUrl}/dashboard";

        if (isApproved)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f4f4f4; }}
        .email-container {{ background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); overflow: hidden; }}
        .email-header {{ background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; padding: 30px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 28px; font-weight: 700; }}
        .email-body {{ padding: 30px; }}
        .congratulations {{ background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%); padding: 20px; border-radius: 8px; text-align: center; margin: 20px 0; }}
        .congratulations h2 {{ color: #92400e; font-size: 24px; margin: 0 0 10px 0; }}
        .message-box {{ background-color: #fffbeb; border-left: 4px solid #f59e0b; padding: 15px 20px; margin: 20px 0; border-radius: 4px; }}
        .action-button {{ display: inline-block; padding: 12px 30px; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white !important; text-decoration: none; border-radius: 6px; font-weight: 600; text-align: center; margin: 20px 0; }}
        .footer {{ background-color: #f8f9fa; padding: 20px 30px; text-align: center; color: #666; font-size: 14px; border-top: 1px solid #e9ecef; }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""email-header"">
            <h1>?? Congratulations!</h1>
        </div>
        
        <div class=""email-body"">
            <p style=""font-size: 16px;"">Hello <strong>{recipientName}</strong>,</p>
            
            <div class=""congratulations"">
                <h2>?? Your Promotion Has Been Approved!</h2>
                <p style=""font-size: 18px; color: #78350f; margin: 10px 0 0 0;"">
                    We are delighted to inform you that your promotion has been processed and approved!
                </p>
            </div>
            
            <div class=""message-box"">
                <p style=""font-size: 16px; margin: 0;"">
                    Your hard work, dedication, and exceptional performance have been recognized. 
                    This promotion is a testament to your contributions to the organization.
                </p>
            </div>

            {(!string.IsNullOrEmpty(comment) ? $@"
            <div style=""background-color: #f3f4f6; padding: 15px; border-radius: 4px; margin: 20px 0;"">
                <p style=""margin: 0; font-weight: 600; color: #374151;"">Message from Management:</p>
                <p style=""margin: 10px 0 0 0; color: #1f2937; font-style: italic;"">{comment}</p>
            </div>" : "")}
            
            <div style=""text-align: center;"">
                <a href=""{actionUrl}"" class=""action-button"">
                    View Your Dashboard
                </a>
            </div>
            
            <p style=""margin-top: 30px; color: #666; font-size: 14px; text-align: center;"">
                HR will contact you shortly with further details about your new role.
            </p>
        </div>
        
        <div class=""footer"">
            <p><strong>Employee Performance Evaluation and Competency Progression System</strong></p>
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";
        }
        else
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f4f4f4; }}
        .email-container {{ background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); overflow: hidden; }}
        .email-header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px; }}
        .message-box {{ background-color: #e0e7ff; border-left: 4px solid #667eea; padding: 15px 20px; margin: 20px 0; border-radius: 4px; }}
        .footer {{ background-color: #f8f9fa; padding: 20px 30px; text-align: center; color: #666; font-size: 14px; border-top: 1px solid #e9ecef; }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""email-header"">
            <h1>?? Evaluation Complete</h1>
        </div>
        
        <div class=""email-body"">
            <p style=""font-size: 16px;"">Hello <strong>{recipientName}</strong>,</p>
            
            <div class=""message-box"">
                <p style=""font-size: 16px; margin: 0;"">
                    Your performance evaluation has been completed. While a promotion is not being offered at this time, 
                    we appreciate your contributions and encourage you to continue your excellent work.
                </p>
            </div>

            {(!string.IsNullOrEmpty(comment) ? $@"
            <div style=""background-color: #f3f4f6; padding: 15px; border-radius: 4px; margin: 20px 0;"">
                <p style=""margin: 0; font-weight: 600; color: #374151;"">Feedback:</p>
                <p style=""margin: 10px 0 0 0; color: #1f2937;"">{comment}</p>
            </div>" : "")}
            
            <p style=""margin-top: 20px; color: #666; font-size: 14px;"">
                Please continue to focus on your development goals. Your manager will work with you 
                on areas for growth and improvement.
            </p>
        </div>
        
        <div class=""footer"">
            <p><strong>Employee Performance Evaluation and Competency Progression System</strong></p>
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";
        }
    }

    private string GetMessageTitle(string action)
    {
        return action switch
        {
            "Submitted" => "New Evaluation Submitted",
            "Pending" => "Action Required",
            "Approved" => "Evaluation Approved",
            "Assigned" => "Peer Review Assignment",
            _ => "Evaluation Notification"
        };
    }

    private string GetMessageContent(string action, string employeeName, string role)
    {
        return action switch
        {
            "Submitted" => $"A new evaluation has been submitted by {employeeName} and requires your review as {role}.",
            "Pending" => $"An evaluation for {employeeName} is pending your action as {role}.",
            "Approved" => $"The evaluation for {employeeName} has been approved and is now awaiting your review as {role}.",
            "Assigned" => $"You have been assigned as a peer reviewer for {employeeName}'s evaluation.",
            _ => $"An evaluation for {employeeName} requires your attention as {role}."
        };
    }

    private string GetActionBadgeClass(string action)
    {
        return action switch
        {
            "Submitted" => "badge-info",
            "Pending" => "badge-warning",
            "Approved" => "badge-success",
            "Assigned" => "badge-info",
            _ => "badge-info"
        };
    }

    /// <summary>
    /// Process queued emails (called by background service)
    /// </summary>
    internal async Task ProcessQueuedEmailsAsync(CancellationToken cancellationToken)
    {
        while (_emailQueue.TryDequeue(out var emailMessage))
        {
            try
            {
                emailMessage.Status = EmailStatus.Sending;
                emailMessage.LastAttemptAt = DateTime.UtcNow;

                await SendEmailAsync(
                    emailMessage.ToEmail,
                    emailMessage.ToName,
                    emailMessage.Subject,
                    emailMessage.HtmlBody,
                    cancellationToken);

                emailMessage.Status = EmailStatus.Sent;
                _logger.LogInformation("Queued email {EmailId} sent successfully to {Email}", 
                    emailMessage.Id, emailMessage.ToEmail);
            }
            catch (Exception ex)
            {
                emailMessage.RetryCount++;
                emailMessage.LastError = ex.Message;

                if (emailMessage.RetryCount < _settings.MaxRetryAttempts)
                {
                    _logger.LogWarning("Failed to send queued email {EmailId} (attempt {Attempt}/{Max}). Requeueing...", 
                        emailMessage.Id, emailMessage.RetryCount, _settings.MaxRetryAttempts);
                    
                    // Requeue for retry
                    await Task.Delay(TimeSpan.FromSeconds(_settings.RetryDelaySeconds), cancellationToken);
                    _emailQueue.Enqueue(emailMessage);
                }
                else
                {
                    emailMessage.Status = EmailStatus.Failed;
                    _logger.LogError(ex, "Failed to send queued email {EmailId} after {Attempts} attempts", 
                        emailMessage.Id, _settings.MaxRetryAttempts);
                }
            }
        }
    }

    /// <summary>
    /// Get queue statistics
    /// </summary>
    internal int GetQueueCount() => _emailQueue.Count;

    #endregion
}
