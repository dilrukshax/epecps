using Epecps.Application.Interfaces;
using Epecps.Application.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Epecps.Infrastructure.Services;

/// <summary>
/// Background service that processes queued emails
/// </summary>
public class EmailBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailBackgroundService> _logger;
    private readonly EmailSettings _settings;
    private readonly TimeSpan _processInterval = TimeSpan.FromSeconds(10); // Process every 10 seconds

    public EmailBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<EmailBackgroundService> logger,
        IOptions<EmailSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.EnableBackgroundProcessing)
        {
            _logger.LogInformation("Email background processing is disabled");
            return;
        }

        _logger.LogInformation("Email Background Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing email queue");
            }

            // Wait before next processing cycle
            await Task.Delay(_processInterval, stoppingToken);
        }

        _logger.LogInformation("Email Background Service is stopping");
    }

    private async Task ProcessEmailsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Check if it's our implementation with queue processing
        if (emailService is EmailService concreteEmailService)
        {
            var queueCount = concreteEmailService.GetQueueCount();
            
            if (queueCount > 0)
            {
                _logger.LogInformation("Processing {Count} queued email(s)", queueCount);
                await concreteEmailService.ProcessQueuedEmailsAsync(cancellationToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email Background Service is stopping gracefully");

        // Process any remaining emails before shutdown
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        
        if (emailService is EmailService concreteEmailService)
        {
            var remainingCount = concreteEmailService.GetQueueCount();
            if (remainingCount > 0)
            {
                _logger.LogInformation("Processing {Count} remaining email(s) before shutdown", remainingCount);
                await concreteEmailService.ProcessQueuedEmailsAsync(cancellationToken);
            }
        }

        await base.StopAsync(cancellationToken);
    }
}
