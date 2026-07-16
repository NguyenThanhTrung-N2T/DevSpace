namespace Auth.Application.Common.Options;

public class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    public string Default { get; set; } = string.Empty;
}
