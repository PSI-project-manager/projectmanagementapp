using Api.Dtos;
using Api.Models;
using Api.Validators;
using Xunit;

namespace Api.Tests.Validators;

public class CreateProjectRequestValidatorTests
{
    private readonly CreateProjectRequestValidator validator = new();

    [Fact]
    public void Validate_FlagsEveryBrokenRule_AndAcceptsValidRequest()
    {
        var empty = validator.Validate(
            new CreateProjectRequest("", Description: null, CreatedByUserId: 0)
        );

        Assert.Equal(
            new[] { "CreatedByUserId", "Name" },
            empty.Errors.Select(e => e.PropertyName).Distinct().Order()
        );

        var tooLong = validator.Validate(
            new CreateProjectRequest(
                new string('n', Project.MaxNameLength + 1),
                new string('d', Project.MaxDescriptionLength + 1),
                CreatedByUserId: 1
            )
        );

        Assert.Equal(
            new[] { "Description", "Name" },
            tooLong.Errors.Select(e => e.PropertyName).Distinct().Order()
        );

        var valid = validator.Validate(
            new CreateProjectRequest("Mobile App", "v1 launch", CreatedByUserId: 1)
        );

        Assert.True(valid.IsValid);
    }
}
