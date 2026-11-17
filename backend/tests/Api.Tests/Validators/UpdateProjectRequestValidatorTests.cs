using Api.Dtos;
using Api.Models;
using Api.Validators;
using Xunit;

namespace Api.Tests.Validators;

public class UpdateProjectRequestValidatorTests
{
    private readonly UpdateProjectRequestValidator validator = new();

    [Fact]
    public void Validate_FlagsEveryBrokenRule_AndAcceptsValidRequest()
    {
        var empty = validator.Validate(
            new UpdateProjectRequest(ProjectId: 0, "", Description: null)
        );

        Assert.Equal(
            new[] { "Name", "ProjectId" },
            empty.Errors.Select(e => e.PropertyName).Distinct().Order()
        );

        var tooLong = validator.Validate(
            new UpdateProjectRequest(
                ProjectId: 1,
                new string('n', Project.MaxNameLength + 1),
                new string('d', Project.MaxDescriptionLength + 1)
            )
        );

        Assert.Equal(
            new[] { "Description", "Name" },
            tooLong.Errors.Select(e => e.PropertyName).Distinct().Order()
        );

        var valid = validator.Validate(
            new UpdateProjectRequest(ProjectId: 1, "Mobile App", "v1 launch")
        );

        Assert.True(valid.IsValid);
    }
}
