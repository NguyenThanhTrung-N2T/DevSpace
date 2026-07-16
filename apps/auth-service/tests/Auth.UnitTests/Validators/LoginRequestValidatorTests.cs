using Auth.Application.Common.Models;
using Xunit;

namespace Auth.UnitTests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Request_Is_Valid()
    {
        var request = new LoginRequest("test@devspace.com", "Password123!");
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public void Should_Fail_When_Email_Is_Invalid(string email)
    {
        var request = new LoginRequest(email, "Password123!");
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginRequest.Email));
    }

    [Fact]
    public void Should_Fail_When_Password_Is_Empty()
    {
        var request = new LoginRequest("test@devspace.com", "");
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginRequest.Password));
    }
}
