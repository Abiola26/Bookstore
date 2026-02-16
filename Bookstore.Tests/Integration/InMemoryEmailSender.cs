using Bookstore.Application.Services;

namespace Bookstore.Tests.Integration;

public class InMemoryEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> SentEmails { get; } = new();

    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        SentEmails.Add((to, subject, htmlBody));
        return Task.CompletedTask;
    }
}
