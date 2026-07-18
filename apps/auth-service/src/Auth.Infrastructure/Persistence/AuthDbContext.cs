using Auth.Application.Common.Interfaces;
using Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

public class AuthDbContext : IdentityDbContext<User, Role, Guid>, IAuthDbContext
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

            // Index soft-delete and active state columns
            entity.HasIndex(u => u.IsActive);
            entity.HasIndex(u => u.DeletedAt);
        });

        builder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("user_claims");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("user_roles");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("user_logins");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("role_claims");
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("identity_user_tokens"); // Renamed to avoid name collisions with our custom user_tokens
        });

        // Custom RefreshToken Entity Configurations
        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.TokenHash).IsRequired();

            // Match global query filter with parent User soft delete
            entity.HasQueryFilter(rt => rt.User.DeletedAt == null);

            // Setup indexing and uniqueness
            entity.HasIndex(rt => rt.TokenHash).IsUnique();
            entity.HasIndex(rt => rt.FamilyId);

            entity.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Map self-referencing relationship with Restrict delete behavior
            entity.HasOne(rt => rt.ReplacedByToken)
                .WithMany()
                .HasForeignKey(rt => rt.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Custom UserVerificationToken Entity Configurations
        builder.Entity<UserVerificationToken>(entity =>
        {
            entity.ToTable("user_tokens");
            entity.HasKey(ut => ut.Id);
            entity.Property(ut => ut.TokenHash).IsRequired();

            // Match global query filter with parent User soft delete
            entity.HasQueryFilter(ut => ut.User.DeletedAt == null);

            // Setup indexing
            entity.HasIndex(ut => ut.TokenHash).IsUnique();
            entity.HasIndex(ut => new { ut.UserId, ut.Type });

            entity.HasOne(ut => ut.User)
                .WithMany(u => u.UserVerificationTokens)
                .HasForeignKey(ut => ut.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
