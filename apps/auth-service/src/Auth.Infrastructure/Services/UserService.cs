using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;
using Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;

    public UserService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new UserInfo(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.EmailConfirmed,
            user.IsActive,
            roles.ToList()
        );
    }

    public async Task<UserInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new UserInfo(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.EmailConfirmed,
            user.IsActive,
            roles.ToList()
        );
    }

    public async Task<UserInfo> CreateUserAsync(string email, string displayName, string password, CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = displayName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new FluentValidation.Results.ValidationFailure(e.Code, e.Description));
            throw new ValidationException(errors);
        }

        // Assign default User role
        await _userManager.AddToRoleAsync(user, "User");

        return new UserInfo(
            user.Id,
            user.Email,
            user.DisplayName,
            user.EmailConfirmed,
            user.IsActive,
            new List<string> { "User" }
        );
    }

    public async Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }
    }
}
