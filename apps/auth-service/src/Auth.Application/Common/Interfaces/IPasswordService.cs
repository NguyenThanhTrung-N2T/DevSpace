namespace Auth.Application.Common.Interfaces;

public interface IPasswordService
{
    Task<bool> CheckPasswordAsync(Guid userId, string password, bool lockoutOnFailure, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken cancellationToken = default);
    Task RunDummyHashCheckAsync(CancellationToken cancellationToken = default);
}
