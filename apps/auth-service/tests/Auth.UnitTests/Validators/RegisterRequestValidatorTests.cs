using Auth.Application.Common.Models;
using Xunit;

namespace Auth.UnitTests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Request_Is_Valid()
    {
        var request = new RegisterRequest("test@devspace.com", "Password123!", "Test User");
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    public void Should_Fail_When_Email_Is_Invalid(string email)
    {
        var request = new RegisterRequest(email, "Password123!", "Test User");
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    [Fact]
    public void Should_Fail_When_DisplayName_Is_Empty()
    {
        var request = new RegisterRequest("test@devspace.com", "Password123!", "");
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.DisplayName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void Should_Fail_When_Password_Is_Invalid(string password)
    {
        var request = new RegisterRequest("test@devspace.com", password, "Test User");
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Password));
    }
}
