using Api.Dtos;
using Api.Models;
using Api.Validators;
using Xunit;

namespace Api.Tests.Validators;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator validator = new();

    [Fact]
    public void Validate_FlagsEveryBrokenRule_AndAcceptsValidRequest()
    {
        var invalid = validator.Validate(new CreateUserRequest("not-an-email", "", "short"));

        Assert.Equal(
            new[] { "Email", "FullName", "Password" },
            invalid.Errors.Select(e => e.PropertyName).Distinct().Order()
        );

        var tooLong = validator.Validate(
            new CreateUserRequest(
                $"{new string('e', User.MaxEmailLength)}@example.com",
                new string('n', User.MaxFullNameLength + 1),
                "password123"
            )
        );

        Assert.Equal(
            new[] { "Email", "FullName" },
            tooLong.Errors.Select(e => e.PropertyName).Distinct().Order()
        );

        var valid = validator.Validate(
            new CreateUserRequest("jane@example.com", "Jane Doe", "password123")
        );

        Assert.True(valid.IsValid);
    }
}
