using Api.Dtos;
using Api.Models;
using Api.Validators;
using Xunit;

namespace Api.Tests.Validators;

public class UpdateUserRequestValidatorTests
{
    private readonly UpdateUserRequestValidator validator = new();

    [Fact]
    public void Validate_FlagsEveryBrokenRule_AndAcceptsValidRequest()
    {
        var invalid = validator.Validate(
            new UpdateUserRequest(UserId: 0, "not-an-email", "", Array.Empty<string>())
        );

        Assert.Equal(
            new[] { "Email", "FullName", "UserId" },
            invalid.Errors.Select(e => e.PropertyName).Distinct().Order()
        );

        var tooLong = validator.Validate(
            new UpdateUserRequest(
                UserId: 1,
                $"{new string('e', User.MaxEmailLength)}@example.com",
                new string('n', User.MaxFullNameLength + 1),
                new[] { "Admin" }
            )
        );

        Assert.Equal(
            new[] { "Email", "FullName" },
            tooLong.Errors.Select(e => e.PropertyName).Distinct().Order()
        );

        var valid = validator.Validate(
            new UpdateUserRequest(UserId: 1, "jane@example.com", "Jane Doe", new[] { "Admin" })
        );

        Assert.True(valid.IsValid);
    }
}
