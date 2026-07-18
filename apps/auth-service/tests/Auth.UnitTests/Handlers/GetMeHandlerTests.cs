using Auth.Application.Authentication.GetMe;
using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Auth.UnitTests.Handlers;

public class GetMeHandlerTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly GetMeHandler _handler;

    public GetMeHandlerTests()
    {
        _handler = new GetMeHandler(_userService);
    }

    [Fact]
    public async Task Should_Return_UserDto_When_User_Exists_And_Is_Active()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userInfo = new UserInfo(
            userId,
            "test@devspace.com",
            "Test User",
            "avatar.png",
            true,
            true,
            new List<string> { "User" }
        );

        _userService.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(userInfo);

        // Act
        var result = await _handler.HandleAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("test@devspace.com", result.Email);
        Assert.Equal("Test User", result.DisplayName);
        Assert.Equal("avatar.png", result.AvatarUrl);
    }

    [Fact]
    public async Task Should_Throw_UnauthorizedException_When_User_Not_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userService.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((UserInfo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.HandleAsync(userId));
    }

    [Fact]
    public async Task Should_Throw_UnauthorizedException_When_User_Is_Inactive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userInfo = new UserInfo(
            userId,
            "test@devspace.com",
            "Test User",
            "avatar.png",
            true,
            false, // Inactive
            new List<string> { "User" }
        );

        _userService.FindByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(userInfo);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.HandleAsync(userId));
    }
}
