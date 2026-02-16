using Bookstore.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net;

namespace Bookstore.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly Bookstore.Application.Settings.EmailSettings _settings;

    public SmtpEmailSender(ILogger<SmtpEmailSender> logger, Microsoft.Extensions.Options.IOptions<Bookstore.Application.Settings.EmailSettings> options)
    {
        _logger = logger;
        _settings = options.Value ?? new Bookstore.Application.Settings.EmailSettings();
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_settings.SmtpHost))
            {
                // Fallback to logging sender
                _logger.LogInformation("SMTP not configured - logging email to {To}: {Subject}\n{Body}", to, subject, htmlBody);
                return;
            }

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress ?? "noreply@example.com", _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mail.To.Add(to);

            await client.SendMailAsync(mail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            // swallow exceptions to not block registration flow; optionally surface via telemetry
        }
    }
}

// settings class moved to Bookstore.Application.Settings.EmailSettings
