using Auth.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Services;

public class MockEmailSender : IEmailSender
{
    private readonly ILogger<MockEmailSender> _logger;

    public MockEmailSender(ILogger<MockEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("Sending email to {To} | Subject: {Subject} | Body: {Body}", to, subject, body);
        return Task.CompletedTask;
    }

    public Task SendEmailVerificationLinkAsync(string to, string link)
    {
        _logger.LogInformation("\n==================================================\n" +
                               "MOCK EMAIL: Email Verification\n" +
                               "To: {To}\n" +
                               "Link: {Link}\n" +
                               "==================================================", to, link);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(string to, string link)
    {
        _logger.LogInformation("\n==================================================\n" +
                               "MOCK EMAIL: Password Reset Request\n" +
                               "To: {To}\n" +
                               "Link: {Link}\n" +
                               "==================================================", to, link);
        return Task.CompletedTask;
    }
}
