using Application.Projects;
using Application.Tests.Fakes;
using FluentValidation;
using Xunit;

namespace Application.Tests.Projects;

public class CreateProjectHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidRequest_AddsProjectToRepository()
    {
        var repository = new FakeProjectRepository();
        var handler = new CreateProjectHandler(repository, new CreateProjectRequestValidator());
        var organizationId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new CreateProjectRequest(organizationId, "New Project", "Description")
        );

        Assert.Equal("New Project", result.Name);
        Assert.Equal(organizationId, result.OrganizationId);

        var stored = await repository.ListByOrganizationAsync(organizationId);
        Assert.Single(stored);
    }

    [Fact]
    public async Task HandleAsync_WithMissingName_ThrowsValidationException()
    {
        var repository = new FakeProjectRepository();
        var handler = new CreateProjectHandler(repository, new CreateProjectRequestValidator());

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.HandleAsync(new CreateProjectRequest(Guid.NewGuid(), "", null))
        );
    }

    [Fact]
    public async Task HandleAsync_WithoutOrganizationId_ThrowsValidationException()
    {
        var repository = new FakeProjectRepository();
        var handler = new CreateProjectHandler(repository, new CreateProjectRequestValidator());

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.HandleAsync(new CreateProjectRequest(Guid.Empty, "Name", null))
        );
    }
}
