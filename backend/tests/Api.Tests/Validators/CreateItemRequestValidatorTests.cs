using Api.Dtos;
using Api.Validators;
using Xunit;

namespace Api.Tests.Validators;

public class CreateItemRequestValidatorTests
{
    private readonly CreateItemRequestValidator validator = new();

    [Fact]
    public void Validate_FlagsEveryBrokenRule_AndAcceptsValidRequest()
    {
        var empty = validator.Validate(
            new CreateItemRequest("", "", ProjectId: 0, ItemTypeId: 0, StatusId: 0)
        );

        Assert.Equal(
            new[] { "Description", "ItemTypeId", "ProjectId", "StatusId", "Title" },
            empty.Errors.Select(e => e.PropertyName).Distinct().Order()
        );

        var tooLong = validator.Validate(
            new CreateItemRequest(
                new string('t', 201),
                "Users cannot log in",
                ProjectId: 1,
                ItemTypeId: 1,
                StatusId: 1
            )
        );

        Assert.Equal(
            new[] { "Title" },
            tooLong.Errors.Select(e => e.PropertyName).Distinct().Order()
        );

        var valid = validator.Validate(
            new CreateItemRequest(
                "Fix login",
                "Users cannot log in",
                ProjectId: 1,
                ItemTypeId: 1,
                StatusId: 1
            )
        );

        Assert.True(valid.IsValid);
    }
}
