namespace Auth.Application.Common.Interfaces;

public interface IEmailVerificationService
{
    Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);
}
