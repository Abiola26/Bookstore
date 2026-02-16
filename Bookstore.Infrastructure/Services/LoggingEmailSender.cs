using Bookstore.Application.Services;
using Microsoft.Extensions.Logging;

namespace Bookstore.Infrastructure.Services;

public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        // For now, log the email. In production, replace with real SMTP or external provider.
        _logger.LogInformation("Sending email to {To}. Subject: {Subject}. Body: {Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
