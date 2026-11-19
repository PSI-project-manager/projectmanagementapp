using Api.Dtos;
using Api.Models;
using Api.Validators;
using Xunit;

namespace Api.Tests.Validators;

public class CreateItemTypeRequestValidatorTests
{
    private readonly CreateItemTypeRequestValidator validator = new();

    [Fact]
    public void Validate_FlagsEveryBrokenRule_AndAcceptsValidRequest()
    {
        var empty = validator.Validate(new CreateItemTypeRequest(""));

        Assert.Equal(new[] { "Name" }, empty.Errors.Select(e => e.PropertyName).Distinct().Order());
        Assert.Equal("Name is required.", Assert.Single(empty.Errors).ErrorMessage);

        var tooLong = validator.Validate(
            new CreateItemTypeRequest(new string('n', Itemtype.MaxNameLength + 1))
        );

        Assert.Equal(
            $"Name cannot exceed {Itemtype.MaxNameLength} characters.",
            Assert.Single(tooLong.Errors).ErrorMessage
        );

        var valid = validator.Validate(new CreateItemTypeRequest("Bug"));

        Assert.True(valid.IsValid);
    }
}
