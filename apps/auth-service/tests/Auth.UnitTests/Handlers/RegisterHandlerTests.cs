using Auth.Application.Authentication.Register;
using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;
using Auth.Application.Common.Options;
using Microsoft.Extensions.Options;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Auth.UnitTests.Handlers;

public class RegisterHandlerTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IEmailVerificationService _emailVerificationService = Substitute.For<IEmailVerificationService>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IOptions<SecurityOptions> _securityOptions;
    private readonly RegisterHandler _handler;

    public RegisterHandlerTests()
    {
        _securityOptions = Options.Create(new SecurityOptions
        {
            PublicOrigin = "https://localhost:5001"
        });

        _handler = new RegisterHandler(_userService, _emailVerificationService, _emailSender, _securityOptions);
    }

    [Fact]
    public async Task Should_CreateUser_And_SendEmail_When_Email_Is_Available()
    {
        // Arrange
        var request = new RegisterRequest("newuser@devspace.com", "Password123!", "New User");
        var userId = Guid.NewGuid();
        var userInfo = new UserInfo(userId, "newuser@devspace.com", "New User", false, true, new[] { "User" });

        _userService.FindByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns((UserInfo?)null);

        _userService.CreateUserAsync(request.Email, request.DisplayName, request.Password, Arg.Any<CancellationToken>())
            .Returns(userInfo);

        _emailVerificationService.GenerateEmailConfirmationTokenAsync(userId, Arg.Any<CancellationToken>())
            .Returns("email-confirm-token");

        // Act
        await _handler.HandleAsync(request);

        // Assert
        await _userService.Received(1).CreateUserAsync(request.Email, request.DisplayName, request.Password, Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendEmailAsync(
            request.Email,
            Arg.Any<string>(),
            Arg.Is<string>(body => body != null && body.Contains("email-confirm-token") && body.Contains("localhost:5001"))
        );
    }

    [Fact]
    public async Task Should_Throw_ConflictException_When_Email_Is_Already_Taken()
    {
        // Arrange
        var request = new RegisterRequest("existing@devspace.com", "Password123!", "Existing User");
        var userInfo = new UserInfo(Guid.NewGuid(), "existing@devspace.com", "Existing User", true, true, new[] { "User" });

        _userService.FindByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns(userInfo);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.HandleAsync(request));

        await _userService.DidNotReceive().CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
