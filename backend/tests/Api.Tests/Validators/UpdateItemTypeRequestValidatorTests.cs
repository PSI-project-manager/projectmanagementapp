using Api.Dtos;
using Api.Models;
using Api.Validators;
using Xunit;

namespace Api.Tests.Validators;

public class UpdateItemTypeRequestValidatorTests
{
    private readonly UpdateItemTypeRequestValidator validator = new();

    [Fact]
    public void Validate_FlagsEveryBrokenRule_AndAcceptsValidRequest()
    {
        var empty = validator.Validate(
            new UpdateItemTypeRequest(ItemTypeId: 0, "", IsActive: true)
        );

        Assert.Equal(
            new[] { "ItemTypeId", "Name" },
            empty.Errors.Select(e => e.PropertyName).Distinct().Order()
        );

        var tooLong = validator.Validate(
            new UpdateItemTypeRequest(
                ItemTypeId: 1,
                new string('n', Itemtype.MaxNameLength + 1),
                IsActive: true
            )
        );

        Assert.Equal(
            $"Name cannot exceed {Itemtype.MaxNameLength} characters.",
            Assert.Single(tooLong.Errors).ErrorMessage
        );

        var valid = validator.Validate(
            new UpdateItemTypeRequest(ItemTypeId: 1, "Bug", IsActive: true)
        );

        Assert.True(valid.IsValid);
    }
}
