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
        new(
            db,
            new CreateProjectRequestValidator(),
            new UpdateProjectRequestValidator(),
            new ProjectAccessRequestValidator()
        );

    private static AppDbContext CreateDb() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options
        );

    private static async Task<Api.Models.User> AddUserAsync(
        AppDbContext db,
        string email,
        bool isActive = true
    )
    {
        var user = new Api.Models.User
        {
            Email = email,
            Passwordhash = "hash",
            Fullname = email,
            Isactive = isActive,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_AddsProject()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        await AddUserAsync(db, "creator@test.com");

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
        await AddUserAsync(db, "creator@test.com");

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
        await AddUserAsync(db, "creator@test.com");

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
    public async Task CreateAsync_GrantsCreatorAccessAutomatically()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var creator = await AddUserAsync(db, "creator@test.com");

        var created = await service.CreateAsync(
            new CreateProjectRequest("Project", null, CreatedByUserId: creator.Userid)
        );

        var access = await service.ListAccessAsync(created.Id);
        Assert.Single(access);
        Assert.Equal(creator.Userid, access[0].UserId);
    }

    [Fact]
    public async Task ListAsync_AsAdmin_ReturnsAllProjectsOrderedByName()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var creator = await AddUserAsync(db, "creator@test.com");

        await service.CreateAsync(
            new CreateProjectRequest("Zebra", null, CreatedByUserId: creator.Userid)
        );
        await service.CreateAsync(
            new CreateProjectRequest("Apple", null, CreatedByUserId: creator.Userid)
        );

        var result = await service.ListAsync(requestingUserId: creator.Userid, isAdmin: true);

        Assert.Equal(2, result.Count);
        Assert.Equal("Apple", result[0].Name);
        Assert.Equal("Zebra", result[1].Name);
    }

    [Fact]
    public async Task ListAsync_WithNoProjects_ReturnsEmptyList()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.ListAsync(requestingUserId: 1, isAdmin: true);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAsync_AsContributor_OnlyReturnsProjectsTheyHaveAccessTo()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var owner = await AddUserAsync(db, "owner@test.com");
        var contributor = await AddUserAsync(db, "contributor@test.com");

        var ownProject = await service.CreateAsync(
            new CreateProjectRequest("Owner's Project", null, CreatedByUserId: owner.Userid)
        );
        await service.CreateAsync(
            new CreateProjectRequest("Other Project", null, CreatedByUserId: owner.Userid)
        );

        await service.GrantAccessAsync(new ProjectAccessRequest(ownProject.Id, contributor.Userid));

        var result = await service.ListAsync(
            requestingUserId: contributor.Userid,
            isAdmin: false
        );

        Assert.Single(result);
        Assert.Equal(ownProject.Id, result[0].Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithoutAccess_ThrowsUnauthorizedAccessException()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var owner = await AddUserAsync(db, "owner@test.com");
        var outsider = await AddUserAsync(db, "outsider@test.com");

        var project = await service.CreateAsync(
            new CreateProjectRequest("Private Project", null, CreatedByUserId: owner.Userid)
        );

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetByIdAsync(project.Id, outsider.Userid, isAdmin: false)
        );
    }

    [Fact]
    public async Task GetByIdAsync_AsAdmin_SucceedsWithoutExplicitAccess()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var owner = await AddUserAsync(db, "owner@test.com");
        var admin = await AddUserAsync(db, "admin@test.com");

        var project = await service.CreateAsync(
            new CreateProjectRequest("Private Project", null, CreatedByUserId: owner.Userid)
        );

        var result = await service.GetByIdAsync(project.Id, admin.Userid, isAdmin: true);

        Assert.Equal(project.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownProjectId_ThrowsKeyNotFoundException()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetByIdAsync(999, requestingUserId: 1, isAdmin: true)
        );
    }

    [Fact]
    public async Task GrantAccessAsync_LetsContributorSeeTheProject()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var owner = await AddUserAsync(db, "owner@test.com");
        var contributor = await AddUserAsync(db, "contributor@test.com");

        var project = await service.CreateAsync(
            new CreateProjectRequest("Project", null, CreatedByUserId: owner.Userid)
        );

        await service.GrantAccessAsync(new ProjectAccessRequest(project.Id, contributor.Userid));

        var result = await service.GetByIdAsync(project.Id, contributor.Userid, isAdmin: false);
        Assert.Equal(project.Id, result.Id);
    }

    [Fact]
    public async Task GrantAccessAsync_IsIdempotent()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var owner = await AddUserAsync(db, "owner@test.com");
        var contributor = await AddUserAsync(db, "contributor@test.com");

        var project = await service.CreateAsync(
            new CreateProjectRequest("Project", null, CreatedByUserId: owner.Userid)
        );

        await service.GrantAccessAsync(new ProjectAccessRequest(project.Id, contributor.Userid));
        await service.GrantAccessAsync(new ProjectAccessRequest(project.Id, contributor.Userid));

        var access = await service.ListAccessAsync(project.Id);

        Assert.Equal(2, access.Count);
        Assert.Contains(access, a => a.UserId == owner.Userid);
        Assert.Contains(access, a => a.UserId == contributor.Userid);
    }

    [Fact]
    public async Task GrantAccessAsync_WithUnknownProject_ThrowsKeyNotFoundException()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var user = await AddUserAsync(db, "user@test.com");

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GrantAccessAsync(new ProjectAccessRequest(999, user.Userid))
        );
    }

    [Fact]
    public async Task GrantAccessAsync_WithUnknownUser_ThrowsKeyNotFoundException()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var owner = await AddUserAsync(db, "owner@test.com");

        var project = await service.CreateAsync(
            new CreateProjectRequest("Project", null, CreatedByUserId: owner.Userid)
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GrantAccessAsync(new ProjectAccessRequest(project.Id, 999))
        );
    }

    [Fact]
    public async Task GrantAccessAsync_WithInactiveUser_ThrowsValidationException()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var owner = await AddUserAsync(db, "owner@test.com");
        var inactiveUser = await AddUserAsync(db, "inactive@test.com", isActive: false);

        var project = await service.CreateAsync(
            new CreateProjectRequest("Project", null, CreatedByUserId: owner.Userid)
        );

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.GrantAccessAsync(new ProjectAccessRequest(project.Id, inactiveUser.Userid))
        );
    }

    [Fact]
    public async Task RevokeAccessAsync_RemovesAccessToTheProject()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var owner = await AddUserAsync(db, "owner@test.com");
        var contributor = await AddUserAsync(db, "contributor@test.com");

        var project = await service.CreateAsync(
            new CreateProjectRequest("Project", null, CreatedByUserId: owner.Userid)
        );
        await service.GrantAccessAsync(new ProjectAccessRequest(project.Id, contributor.Userid));

        await service.RevokeAccessAsync(new ProjectAccessRequest(project.Id, contributor.Userid));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetByIdAsync(project.Id, contributor.Userid, isAdmin: false)
        );
    }

    [Fact]
    public async Task RevokeAccessAsync_WhenNoAccessExists_IsIdempotent()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var owner = await AddUserAsync(db, "owner@test.com");
        var contributor = await AddUserAsync(db, "contributor@test.com");

        var project = await service.CreateAsync(
            new CreateProjectRequest("Project", null, CreatedByUserId: owner.Userid)
        );

        await service.RevokeAccessAsync(new ProjectAccessRequest(project.Id, contributor.Userid));

        var access = await service.ListAccessAsync(project.Id);
        Assert.Single(access); // only the creator
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_PersistsChanges()
    {
        using var db = CreateDb();
        var service = CreateService(db);
        var owner = await AddUserAsync(db, "owner@test.com");
        var created = await service.CreateAsync(
            new CreateProjectRequest("Old Name", "Old description", CreatedByUserId: owner.Userid)
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
        var owner = await AddUserAsync(db, "owner@test.com");
        var created = await service.CreateAsync(
            new CreateProjectRequest("Old Name", null, CreatedByUserId: owner.Userid)
        );

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(new UpdateProjectRequest(created.Id, "", null))
        );
    }
}