using Api.Dtos;
using Api.Validators;
using Xunit;

namespace Api.Tests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator validator = new();

    [Fact]
    public void Validate_FlagsEveryBrokenRule_AndAcceptsValidRequest()
    {
        var empty = validator.Validate(new LoginRequest("", ""));

        Assert.Equal(
            new[] { "Email", "Password" },
            empty.Errors.Select(e => e.PropertyName).Distinct().Order()
        );

        var malformed = validator.Validate(new LoginRequest("not-an-email", "short"));

        Assert.Equal(
            new[] { "Email", "Password" },
            malformed.Errors.Select(e => e.PropertyName).Distinct().Order()
        );
        Assert.Equal(
            new[] { "Invalid email address.", "Password must be at least 8 characters long." },
            malformed.Errors.Select(e => e.ErrorMessage).Order()
        );

        var valid = validator.Validate(new LoginRequest("jane@example.com", "password123"));

        Assert.True(valid.IsValid);
    }
}
