namespace Auth.Application.Common.Options;

public class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int LoginPermitLimit { get; set; } = 5;
    public int LoginQueueLimit { get; set; } = 0;
    public int RegisterPermitLimit { get; set; } = 3;
    public int RegisterQueueLimit { get; set; } = 0;
}
