using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;
using Auth.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IAuthDbContext _dbContext;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<User> userManager,
        IAuthDbContext dbContext,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtService = jwtService;
        _logger = logger;
    }

    [EnableRateLimiting("register")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var validator = new RegisterRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { Errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { Message = "Email is already registered." });
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        // Add to default role
        await _userManager.AddToRoleAsync(user, "User");

        // Generate email verification token (mocked email)
        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // Store verification token in DB for validation/audit
        var verificationToken = new UserVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = UserVerificationTokenType.EmailVerification,
            TokenHash = HashToken(emailToken),
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        _dbContext.UserVerificationTokens.Add(verificationToken);
        await _dbContext.SaveChangesAsync();

        // Print mock verification link to logs
        var verificationLink = $"{Request.Scheme}://{Request.Host}/api/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(emailToken)}";
        _logger.LogInformation("\n==================================================\n" +
                               "MOCK EMAIL: Confirm Registration\n" +
                               "To: {Email}\n" +
                               "Link: {Link}\n" +
                               "==================================================", user.Email, verificationLink);

        return Ok(new { Message = "User registered successfully. Please verify your email via the link printed to logs." });
    }

    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validator = new LoginRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { Errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive || user.DeletedAt != null)
        {
            return Unauthorized(new { Message = "Invalid credentials or account is deactivated." });
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        // Check if email confirmation is required
        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            return Unauthorized(new { Message = "Please confirm your email address before logging in." });
        }

        var roles = await _userManager.GetRolesAsync(user);

        // Generate tokens
        var (accessToken, accessTokenId, accessExpiry) = _jwtService.GenerateAccessToken(user, roles);
        var (refreshTokenStr, refreshTokenId, familyId) = _jwtService.GenerateRefreshToken(user.Id);

        // Save refresh token to db
        var hashedToken = HashToken(refreshTokenStr);
        var refreshTokenEntity = new RefreshToken
        {
            Id = refreshTokenId,
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = hashedToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Device = HttpContext.Request.Headers["User-Agent"].ToString() ?? "unknown",
            IsRevoked = false
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);

        // Update user metrics
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _dbContext.SaveChangesAsync();

        var userDto = new UserDto(user.Id, user.Email!, user.DisplayName, user.AvatarUrl);
        var expiresIn = (int)(accessExpiry - DateTime.UtcNow).TotalSeconds;

        return Ok(new AuthResponse(accessToken, expiresIn, refreshTokenStr, userDto));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var validator = new RefreshTokenRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { Errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var hashedInputToken = HashToken(request.RefreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hashedInputToken);

        if (storedToken == null)
        {
            return Unauthorized(new { Message = "Invalid refresh token." });
        }

        // REUSE DETECTION: If token is already revoked or expired
        if (storedToken.IsRevoked || storedToken.RevokedAt != null || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            if (storedToken.IsRevoked || storedToken.RevokedAt != null)
            {
                _logger.LogWarning("Reuse detected for refresh token {TokenId} in family {FamilyId}. Revoking entire family.", storedToken.Id, storedToken.FamilyId);
                
                // Revoke all tokens in the family immediately
                var familyTokens = await _dbContext.RefreshTokens
                    .Where(t => t.FamilyId == storedToken.FamilyId && !t.IsRevoked && t.RevokedAt == null)
                    .ToListAsync();

                foreach (var token in familyTokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                    token.Reason = $"Revoked due to detected reuse of token {storedToken.Id}";
                }

                await _dbContext.SaveChangesAsync();
            }

            return Unauthorized(new { Message = "Refresh token expired or invalid." });
        }

        var user = storedToken.User;
        if (user == null || !user.IsActive || user.DeletedAt != null)
        {
            return Unauthorized(new { Message = "Account is inactive or deleted." });
        }

        // Revoke the used token
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.Reason = "Replaced by token rotation";

        // Generate new tokens
        var roles = await _userManager.GetRolesAsync(user);
        var (newAccessToken, _, accessExpiry) = _jwtService.GenerateAccessToken(user, roles);
        var (newRefreshTokenStr, newRefreshTokenId, familyId) = _jwtService.GenerateRefreshToken(user.Id, storedToken.FamilyId);

        var newHashedToken = HashToken(newRefreshTokenStr);
        var newRefreshTokenEntity = new RefreshToken
        {
            Id = newRefreshTokenId,
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = newHashedToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Device = HttpContext.Request.Headers["User-Agent"].ToString() ?? "unknown",
            IsRevoked = false
        };

        // Link old token to the replacement
        storedToken.ReplacedByTokenId = newRefreshTokenId;

        _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        var userDto = new UserDto(user.Id, user.Email!, user.DisplayName, user.AvatarUrl);
        var expiresIn = (int)(accessExpiry - DateTime.UtcNow).TotalSeconds;

        return Ok(new AuthResponse(newAccessToken, expiresIn, newRefreshTokenStr, userDto));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var hashedInputToken = HashToken(request.RefreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hashedInputToken);

        if (storedToken != null && !storedToken.IsRevoked && storedToken.RevokedAt == null)
        {
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.Reason = "User logged out";
            await _dbContext.SaveChangesAsync();
        }

        return Ok(new { Message = "Logged out successfully." });
    }

    [Authorize]
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var activeTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked && t.RevokedAt == null)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.Reason = "Revoked by user session termination request";
        }

        await _dbContext.SaveChangesAsync();
        return Ok(new { Message = "All active refresh sessions revoked successfully." });
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return BadRequest(new { Message = "User ID and Token are required." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        // Sync local flag
        user.EmailVerified = true;
        await _userManager.UpdateAsync(user);

        // Complete stored verification token status
        var hashedToken = HashToken(token);
        var dbToken = await _dbContext.UserVerificationTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.TokenHash == hashedToken && t.Type == UserVerificationTokenType.EmailVerification);

        if (dbToken != null)
        {
            dbToken.UsedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        return Ok(new { Message = "Email confirmed successfully. You can now log in." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive || user.DeletedAt != null)
        {
            return Unauthorized(new { Message = "User is inactive or deleted." });
        }

        var userDto = new UserDto(user.Id, user.Email!, user.DisplayName, user.AvatarUrl);
        return Ok(userDto);
    }

    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var validator = new ChangePasswordRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { Errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive || user.DeletedAt != null)
        {
            return Unauthorized();
        }

        var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        return Ok(new { Message = "Password changed successfully." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var validator = new ForgotPasswordRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { Errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        // Prevent email enumeration attacks by returning standard success even if user not found
        if (user != null && user.IsActive && user.DeletedAt == null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetToken = new UserVerificationToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = UserVerificationTokenType.PasswordReset,
                TokenHash = HashToken(token),
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };
            _dbContext.UserVerificationTokens.Add(resetToken);
            await _dbContext.SaveChangesAsync();

            var resetLink = $"{Request.Scheme}://{Request.Host}/api/auth/reset-password?userId={user.Id}&token={Uri.EscapeDataString(token)}";
            _logger.LogInformation("\n==================================================\n" +
                                   "MOCK EMAIL: Reset Password Request\n" +
                                   "To: {Email}\n" +
                                   "Link: {Link}\n" +
                                   "==================================================", user.Email, resetLink);
        }

        return Ok(new { Message = "If the email is registered, a password reset link has been printed to the service logs." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var validator = new ResetPasswordRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { Errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null || !user.IsActive || user.DeletedAt != null)
        {
            return BadRequest(new { Message = "Invalid user or request." });
        }

        var hashedToken = HashToken(request.Token);
        var dbToken = await _dbContext.UserVerificationTokens
            .FirstOrDefaultAsync(t => t.UserId == user.Id && t.TokenHash == hashedToken && t.Type == UserVerificationTokenType.PasswordReset);

        if (dbToken == null || dbToken.UsedAt != null || dbToken.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new { Message = "Invalid or expired token." });
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        dbToken.UsedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new { Message = "Password reset successfully. You can now log in." });
    }

    [HttpPost("resend-email")]
    public async Task<IActionResult> ResendEmail([FromBody] ResendEmailRequest request)
    {
        var validator = new ResendEmailRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { Errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive || user.DeletedAt != null)
        {
            return Ok(new { Message = "If the email is registered and unverified, a verification link has been printed to the service logs." });
        }

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            return BadRequest(new { Message = "Email address is already verified." });
        }

        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var verificationToken = new UserVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = UserVerificationTokenType.EmailVerification,
            TokenHash = HashToken(emailToken),
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        _dbContext.UserVerificationTokens.Add(verificationToken);
        await _dbContext.SaveChangesAsync();

        var verificationLink = $"{Request.Scheme}://{Request.Host}/api/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(emailToken)}";
        _logger.LogInformation("\n==================================================\n" +
                               "MOCK EMAIL: Re-sent Email Verification\n" +
                               "To: {Email}\n" +
                               "Link: {Link}\n" +
                               "==================================================", user.Email, verificationLink);

        return Ok(new { Message = "If the email is registered and unverified, a verification link has been printed to the service logs." });
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
