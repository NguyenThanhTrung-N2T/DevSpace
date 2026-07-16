namespace Auth.Application.Common.Options;

public class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
