using Auth.Application.Common.Interfaces;
using Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

public class AuthDbContext : IdentityDbContext<User, Role, string>, IAuthDbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserVerificationToken> UserVerificationTokens => Set<UserVerificationToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Set default schema to auth
        builder.HasDefaultSchema("auth");

        // Rename Identity Tables to clean lowercase names
        builder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            // Apply Soft Delete Global Query Filter
            entity.HasQueryFilter(u => u.DeletedAt == null);
        });

        builder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("user_claims");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("user_roles");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("user_logins");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("role_claims");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("identity_user_tokens"); // Renamed to avoid name collisions with our custom user_tokens
        });

        // Custom RefreshToken Entity Configurations
        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.TokenHash).IsRequired();

            entity.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Custom UserVerificationToken Entity Configurations
        builder.Entity<UserVerificationToken>(entity =>
        {
            entity.ToTable("user_tokens");
            entity.HasKey(ut => ut.Id);
            entity.Property(ut => ut.TokenHash).IsRequired();

            entity.HasOne(ut => ut.User)
                .WithMany(u => u.UserVerificationTokens)
                .HasForeignKey(ut => ut.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
