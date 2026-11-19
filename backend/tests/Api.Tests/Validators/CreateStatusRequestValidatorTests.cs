using Api.Dtos;
using Api.Models;
using Api.Validators;
using Xunit;

namespace Api.Tests.Validators;

public class CreateStatusRequestValidatorTests
{
    private readonly CreateStatusRequestValidator validator = new();

    [Fact]
    public void Validate_FlagsEveryBrokenRule_AndAcceptsValidRequest()
    {
        var invalid = validator.Validate(new CreateStatusRequest("", SortOrder: -1));

        Assert.Equal(
            new[] { "Name", "SortOrder" },
            invalid.Errors.Select(e => e.PropertyName).Distinct().Order()
        );

        var tooLong = validator.Validate(
            new CreateStatusRequest(new string('n', Status.MaxNameLength + 1))
        );

        Assert.Equal(
            $"Name cannot exceed {Status.MaxNameLength} characters.",
            Assert.Single(tooLong.Errors).ErrorMessage
        );

        // the sort order rule only applies when a value was supplied
        var withoutSortOrder = validator.Validate(new CreateStatusRequest("To Do"));

        Assert.True(withoutSortOrder.IsValid);

        var valid = validator.Validate(new CreateStatusRequest("To Do", SortOrder: 1));

        Assert.True(valid.IsValid);
    }
}
