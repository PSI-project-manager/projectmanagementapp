using Application.Projects;
using Application.Tests.Fakes;
using Xunit;

namespace Application.Tests.Projects;

public class ListProjectsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsOnlyProjectsForRequestedOrganization()
    {
        var repository = new FakeProjectRepository();
        var createHandler = new CreateProjectHandler(repository, new CreateProjectRequestValidator());
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();

        await createHandler.HandleAsync(new CreateProjectRequest(organizationId, "Project A", null));
        await createHandler.HandleAsync(new CreateProjectRequest(organizationId, "Project B", null));
        await createHandler.HandleAsync(new CreateProjectRequest(otherOrganizationId, "Project C", null));

        var handler = new ListProjectsHandler(repository);
        var result = await handler.HandleAsync(organizationId);

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal(organizationId, p.OrganizationId));
    }

    [Fact]
    public async Task HandleAsync_WithNoProjects_ReturnsEmptyList()
    {
        var repository = new FakeProjectRepository();
        var handler = new ListProjectsHandler(repository);

        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.Empty(result);
    }
}
