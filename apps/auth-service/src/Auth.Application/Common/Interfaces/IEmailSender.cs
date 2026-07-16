namespace Auth.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendEmailVerificationLinkAsync(string to, string link);
    Task SendPasswordResetLinkAsync(string to, string link);
}
