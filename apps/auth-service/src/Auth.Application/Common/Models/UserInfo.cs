namespace Auth.Application.Common.Models;

public sealed record UserInfo(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    bool EmailConfirmed,
    bool IsActive,
    IReadOnlyList<string> Roles);
