namespace Auth.Application.Common.Options;

public class SecurityOptions
{
    public const string SectionName = "Security";

    public int PasswordMinLength { get; set; } = 8;
    public bool PasswordRequireUppercase { get; set; } = true;
    public bool PasswordRequireLowercase { get; set; } = true;
    public bool PasswordRequireDigit { get; set; } = true;
    public bool PasswordRequireNonAlphanumeric { get; set; } = false;

    public int PasswordResetExpiryMinutes { get; set; } = 30;
    public int EmailVerificationExpiryHours { get; set; } = 24;
}
