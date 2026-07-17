namespace Auth.Domain.Entities;

public enum UserVerificationTokenType
{
    PasswordReset,
    EmailVerification
}

public class UserVerificationToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public UserVerificationTokenType Type { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAt { get; set; }
}
