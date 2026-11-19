using Api.Dtos;
using Api.Models;
using Api.Validators;
using Xunit;

namespace Api.Tests.Validators;

public class UpdateStatusRequestValidatorTests
{
    private readonly UpdateStatusRequestValidator validator = new();

    [Fact]
    public void Validate_FlagsEveryBrokenRule_AndAcceptsValidRequest()
    {
        var invalid = validator.Validate(
            new UpdateStatusRequest(StatusId: 0, "", SortOrder: -1, IsActive: true)
        );

        Assert.Equal(
            new[] { "Name", "SortOrder", "StatusId" },
            invalid.Errors.Select(e => e.PropertyName).Distinct().Order()
        );

        var tooLong = validator.Validate(
            new UpdateStatusRequest(
                StatusId: 1,
                new string('n', Status.MaxNameLength + 1),
                SortOrder: 0,
                IsActive: true
            )
        );

        Assert.Equal(
            $"Name cannot exceed {Status.MaxNameLength} characters.",
            Assert.Single(tooLong.Errors).ErrorMessage
        );

        var valid = validator.Validate(
            new UpdateStatusRequest(StatusId: 1, "To Do", SortOrder: 0, IsActive: true)
        );

        Assert.True(valid.IsValid);
    }
}
