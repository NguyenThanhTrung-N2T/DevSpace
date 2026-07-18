using Auth.Application.Authentication.Login;
using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;
using FluentValidation;
using FluentValidation.Results;
using ValidationException = Auth.Application.Common.Exceptions.ValidationException;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Auth.UnitTests.Handlers;

public class LoginHandlerTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IPasswordService _passwordService = Substitute.For<IPasswordService>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IRefreshTokenService _refreshTokenService = Substitute.For<IRefreshTokenService>();
    private readonly IValidator<LoginRequest> _validator = Substitute.For<IValidator<LoginRequest>>();
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _validator.ValidateAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult()); // Default is valid

        _handler = new LoginHandler(_userService, _passwordService, _jwtService, _refreshTokenService, _validator);
    }

    [Fact]
    public async Task Should_Return_AuthResponse_When_Credentials_Are_Valid()
    {
        // Arrange
        var request = new LoginRequest("user@devspace.com", "Password123!");
        var userId = Guid.NewGuid();
        var userInfo = new UserInfo(userId, "user@devspace.com", "User Name", null, true, true, new List<string> { "User" });

        _userService.FindByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns(userInfo);

        _passwordService.CheckPasswordAsync(userId, request.Password, true, Arg.Any<CancellationToken>())
            .Returns(true);

        var accessTokenResult = new AccessTokenResult("access-token-string", DateTime.UtcNow.AddMinutes(15));
        _jwtService.GenerateAccessToken(userInfo).Returns(accessTokenResult);

        var refreshTokenResult = new RefreshTokenResult("raw-refresh-token", Guid.NewGuid(), DateTime.UtcNow.AddDays(7), userInfo);
        _refreshTokenService.CreateTokenAsync(userId, "127.0.0.1", "userAgent", Arg.Any<CancellationToken>())
            .Returns(refreshTokenResult);

        // Act
        var response = await _handler.HandleAsync(request, "127.0.0.1", "userAgent");

        // Assert
        Assert.NotNull(response);
        Assert.Equal("access-token-string", response.AccessToken);
        Assert.Equal("raw-refresh-token", response.RefreshToken);
        Assert.Equal(userId, response.User.Id);

        await _userService.Received(1).UpdateLastLoginAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Throw_ValidationException_When_Request_Is_Invalid()
    {
        // Arrange
        var request = new LoginRequest("", "");
        var validationFailure = new ValidationFailure("Email", "Email is required.");
        _validator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { validationFailure }));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => 
            _handler.HandleAsync(request, "127.0.0.1", "userAgent"));
    }

    [Fact]
    public async Task Should_Throw_UnauthorizedException_And_Run_DummyCheck_When_User_Not_Found()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@devspace.com", "Password123!");

        _userService.FindByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns((UserInfo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => 
            _handler.HandleAsync(request, "127.0.0.1", "userAgent"));

        // Ensure dummy PBKDF2 check was run to prevent timing attack leaks
        await _passwordService.Received(1).RunDummyHashCheckAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Throw_UnauthorizedException_When_Password_Is_Incorrect()
    {
        // Arrange
        var request = new LoginRequest("user@devspace.com", "WrongPassword!");
        var userId = Guid.NewGuid();
        var userInfo = new UserInfo(userId, "user@devspace.com", "User Name", null, true, true, new List<string> { "User" });

        _userService.FindByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns(userInfo);

        _passwordService.CheckPasswordAsync(userId, request.Password, true, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => 
            _handler.HandleAsync(request, "127.0.0.1", "userAgent"));

        await _userService.DidNotReceive().UpdateLastLoginAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
