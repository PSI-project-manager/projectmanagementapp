using Application.Projects;
using Application.Tests.Fakes;
using FluentValidation;
using Xunit;

namespace Application.Tests.Projects;

public class UpdateProjectHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidRequest_PersistsChanges()
    {
        var repository = new FakeProjectRepository();
        var created = await new CreateProjectHandler(
            repository,
            new CreateProjectRequestValidator()
        ).HandleAsync(new CreateProjectRequest(Guid.NewGuid(), "Old Name", "Old description"));
        var handler = new UpdateProjectHandler(repository, new UpdateProjectRequestValidator());

        var result = await handler.HandleAsync(
            new UpdateProjectRequest(created.Id, "New Name", "New description")
        );

        Assert.Equal("New Name", result.Name);
        Assert.Equal("New description", result.Description);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownProjectId_ThrowsKeyNotFoundException()
    {
        var repository = new FakeProjectRepository();
        var handler = new UpdateProjectHandler(repository, new UpdateProjectRequestValidator());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.HandleAsync(new UpdateProjectRequest(Guid.NewGuid(), "Name", null))
        );
    }

    [Fact]
    public async Task HandleAsync_WithMissingName_ThrowsValidationException()
    {
        var repository = new FakeProjectRepository();
        var created = await new CreateProjectHandler(
            repository,
            new CreateProjectRequestValidator()
        ).HandleAsync(new CreateProjectRequest(Guid.NewGuid(), "Old Name", null));
        var handler = new UpdateProjectHandler(repository, new UpdateProjectRequestValidator());

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.HandleAsync(new UpdateProjectRequest(created.Id, "", null))
        );
    }
}
