using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Application.Common.Interfaces;

public interface IAuthDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserVerificationToken> UserVerificationTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
