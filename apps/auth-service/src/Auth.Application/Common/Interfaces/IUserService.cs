using Auth.Application.Common.Models;

namespace Auth.Application.Common.Interfaces;

public interface IUserService
{
    Task<UserInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserInfo> CreateUserAsync(string email, string displayName, string password, CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default);
}
