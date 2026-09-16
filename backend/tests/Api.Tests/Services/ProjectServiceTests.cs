using Api.Data;
using Api.Dtos;
using Api.Services;
using Api.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Services;

public class ProjectServiceTests
{
    private static ProjectService CreateService(AppDbContext db) =>
        new(db, new CreateProjectRequestValidator(), new UpdateProjectRequestValidator());

    private static AppDbContext CreateDb() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );

    [Fact]
    public async Task CreateAsync_WithValidRequest_AddsProject()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.CreateAsync(
            new CreateProjectRequest("New Project", "Description", CreatedByUserId: 1)
        );

        Assert.Equal("New Project", result.Name);
        Assert.Equal(1, result.CreatedByUserId);
        Assert.Single(await db.Projects.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_TrimsNameAndDescription()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.CreateAsync(
            new CreateProjectRequest("  Mobile App  ", "  v1 launch  ", CreatedByUserId: 1)
        );

        Assert.Equal("Mobile App", result.Name);
        Assert.Equal("v1 launch", result.Description);
    }

    [Fact]
    public async Task CreateAsync_WithBlankDescription_LeavesDescriptionNull()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.CreateAsync(
            new CreateProjectRequest("Solo Project", "   ", CreatedByUserId: 1)
        );

        Assert.Null(result.Description);
    }

    [Fact]
    public async Task CreateAsync_WithMissingName_ThrowsValidationException()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateProjectRequest("", null, CreatedByUserId: 1))
        );
    }

    [Fact]
    public async Task CreateAsync_WithoutCreatedByUserId_ThrowsValidationException()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateProjectRequest("Name", null, CreatedByUserId: 0))
        );
    }

    [Fact]
    public async Task ListAsync_ReturnsAllProjectsOrderedByName()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await service.CreateAsync(new CreateProjectRequest("Zebra", null, CreatedByUserId: 1));
        await service.CreateAsync(new CreateProjectRequest("Apple", null, CreatedByUserId: 1));

        var result = await service.ListAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Apple", result[0].Name);
        Assert.Equal("Zebra", result[1].Name);
    }

    [Fact]
    public async Task ListAsync_WithNoProjects_ReturnsEmptyList()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.ListAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_PersistsChanges()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var created = await service.CreateAsync(
            new CreateProjectRequest("Old Name", "Old description", CreatedByUserId: 1)
        );

        var result = await service.UpdateAsync(
            new UpdateProjectRequest(created.Id, "New Name", "New description")
        );

        Assert.Equal("New Name", result.Name);
        Assert.Equal("New description", result.Description);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownProjectId_ThrowsKeyNotFoundException()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateAsync(new UpdateProjectRequest(999, "Name", null))
        );
    }

    [Fact]
    public async Task UpdateAsync_WithMissingName_ThrowsValidationException()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var created = await service.CreateAsync(
            new CreateProjectRequest("Old Name", null, CreatedByUserId: 1)
        );

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(new UpdateProjectRequest(created.Id, "", null))
        );
    }
}
