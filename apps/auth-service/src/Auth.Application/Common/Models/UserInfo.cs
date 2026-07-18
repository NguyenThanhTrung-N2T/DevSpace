namespace Auth.Application.Common.Models;

public sealed record UserInfo(
    Guid Id,
    string Email,
    string DisplayName,
    bool EmailConfirmed,
    bool IsActive,
    IReadOnlyList<string> Roles);
