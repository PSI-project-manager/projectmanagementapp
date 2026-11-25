using Api.Dtos;
using Api.Exceptions;
using Api.Services;
using Api.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Integration;

// ProjectService resolved from the real DI container and run against PostgreSQL
public class ProjectServiceTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    private ProjectService Service => Resolve<ProjectService>();

    // ListAsync

    [Fact]
    public async Task ListAsync_AsAdmin_ReturnsEveryProjectOrderedByName()
    {
        var admin = await Data.CreateAdminAsync();
        var contributor = await Data.CreateContributorAsync();
        await Data.CreateProjectAsync(contributor, "Charlie", contributor);
        await Data.CreateProjectAsync(contributor, "Alpha", contributor);
        await Data.CreateProjectAsync(contributor, "Bravo");

        var projects = await Service.ListAsync(admin.Id, currentUserIsAdmin: true);

        Assert.Equal(["Alpha", "Bravo", "Charlie"], projects.Select(p => p.Name));
    }

    [Fact]
    public async Task ListAsync_AsNonAdmin_ReturnsOnlyProjectsTheUserIsAMemberOf()
    {
        var admin = await Data.CreateAdminAsync();
        var member = await Data.CreateContributorAsync();
        var other = await Data.CreateContributorAsync();
        var mine = await Data.CreateProjectAsync(admin, "Mine", member);
        await Data.CreateProjectAsync(admin, "Someone else's", other);
        await Data.CreateProjectAsync(admin, "Nobody's");

        var projects = await Service.ListAsync(member.Id, currentUserIsAdmin: false);

        var project = Assert.Single(projects);
        Assert.Equal(mine.Projectid, project.Id);
    }

    // CreateAsync

    [Fact]
    public async Task CreateAsync_PersistsProject_AndMakesTheCreatorAMember()
    {
        var admin = await Data.CreateAdminAsync();

        var created = await Service.CreateAsync(
            new CreateProjectRequest("  Mobile App  ", "  v1 launch  ", admin.Id)
        );

        Assert.True(created.Id > 0);
        Assert.Equal("Mobile App", created.Name);
        Assert.Equal("v1 launch", created.Description);
        Assert.True(created.IsActive);
        Assert.Equal(admin.Id, created.CreatedByUserId);

        var stored = await Data.WithDbAsync(db =>
            db.Projects.Include(p => p.Projectusers).SingleAsync()
        );
        Assert.Equal(created.Id, stored.Projectid);
        Assert.Equal("Mobile App", stored.Name);
        Assert.Equal(admin.Id, Assert.Single(stored.Projectusers).Userid);
    }

    [Fact]
    public async Task CreateAsync_StoresBlankDescriptionAsNull()
    {
        var admin = await Data.CreateAdminAsync();

        var created = await Service.CreateAsync(
            new CreateProjectRequest("Project", "   ", admin.Id)
        );

        Assert.Null(created.Description);
        Assert.Null(
            await Data.WithDbAsync(db => db.Projects.Select(p => p.Description).SingleAsync())
        );
    }

    // UpdateAsync

    [Fact]
    public async Task UpdateAsync_AsAdmin_UpdatesAndPersistsTrimmedValues()
    {
        var admin = await Data.CreateAdminAsync();
        var project = await Data.CreateProjectAsync(admin, "Old name");

        var updated = await Service.UpdateAsync(
            new UpdateProjectRequest(project.Projectid, "  New name ", "  New description "),
            admin.Id,
            currentUserIsAdmin: true
        );

        Assert.Equal("New name", updated.Name);
        Assert.Equal("New description", updated.Description);

        var stored = await Data.WithDbAsync(db => db.Projects.SingleAsync());
        Assert.Equal("New name", stored.Name);
        Assert.Equal("New description", stored.Description);
    }

    [Fact]
    public async Task UpdateAsync_AsNonAdminMember_IsAllowed()
    {
        var admin = await Data.CreateAdminAsync();
        var member = await Data.CreateContributorAsync();
        var project = await Data.CreateProjectAsync(admin, "Old name", member);

        var updated = await Service.UpdateAsync(
            new UpdateProjectRequest(project.Projectid, "New name", null),
            member.Id,
            currentUserIsAdmin: false
        );

        Assert.Equal("New name", updated.Name);
    }

    [Fact]
    public async Task UpdateAsync_AsNonAdminNonMember_ThrowsKeyNotFound_AndChangesNothing()
    {
        var admin = await Data.CreateAdminAsync();
        var outsider = await Data.CreateContributorAsync();
        var project = await Data.CreateProjectAsync(admin, "Old name");

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            Service.UpdateAsync(
                new UpdateProjectRequest(project.Projectid, "Hijacked", null),
                outsider.Id,
                currentUserIsAdmin: false
            )
        );

        Assert.Equal(
            "Old name",
            await Data.WithDbAsync(db => db.Projects.Select(p => p.Name).SingleAsync())
        );
    }

    // ListUsersAsync

    [Fact]
    public async Task ListUsersAsync_ReturnsOnlyMembers_OrderedByFullName_WithTheirRoles()
    {
        var admin = await Data.CreateAdminAsync("Zed Admin");
        var both = await Data.CreateUserAsync(["Admin", "Contributor"], "Anna Both");
        var contributor = await Data.CreateContributorAsync("Mike Contributor");
        await Data.CreateContributorAsync("Not A Member");
        var project = await Data.CreateProjectAsync(admin, "Project", admin, both, contributor);

        var users = await Service.ListUsersAsync(project.Projectid, CancellationToken.None);

        Assert.Equal(["Anna Both", "Mike Contributor", "Zed Admin"], users.Select(u => u.FullName));
        Assert.Equal(["Admin", "Contributor"], users[0].Roles.Order());
        Assert.Equal(["Contributor"], users[1].Roles);
        Assert.Equal(both.Email, users[0].Email);
    }

    // AddUserAsync

    [Fact]
    public async Task AddUserAsync_AsAdmin_AddsTheMembership()
    {
        var admin = await Data.CreateAdminAsync();
        var contributor = await Data.CreateContributorAsync();
        var project = await Data.CreateProjectAsync(admin, "Project");

        await Service.AddUserAsync(
            contributor.Id,
            project.Projectid,
            clientIsAdmin: true,
            CancellationToken.None
        );

        Assert.True(await IsMemberAsync(project.Projectid, contributor.Id));
    }

    [Fact]
    public async Task AddUserAsync_AsNonAdmin_ThrowsPermissionDenied_AndAddsNothing()
    {
        var admin = await Data.CreateAdminAsync();
        var contributor = await Data.CreateContributorAsync();
        var project = await Data.CreateProjectAsync(admin, "Project");

        await Assert.ThrowsAsync<PermissionDeniedException>(() =>
            Service.AddUserAsync(
                contributor.Id,
                project.Projectid,
                clientIsAdmin: false,
                CancellationToken.None
            )
        );

        Assert.False(await IsMemberAsync(project.Projectid, contributor.Id));
    }

    // RemoveUserAsync

    [Fact]
    public async Task RemoveUserAsync_AsAdmin_RemovesOnlyThatMembership()
    {
        var admin = await Data.CreateAdminAsync();
        var leaving = await Data.CreateContributorAsync();
        var staying = await Data.CreateContributorAsync();
        var project = await Data.CreateProjectAsync(admin, "Project", leaving, staying);

        await Service.RemoveUserAsync(
            leaving.Id,
            project.Projectid,
            clientIsAdmin: true,
            CancellationToken.None
        );

        Assert.False(await IsMemberAsync(project.Projectid, leaving.Id));
        Assert.True(await IsMemberAsync(project.Projectid, staying.Id));
        // only the membership goes, never the user
        Assert.True(await Data.WithDbAsync(db => db.Users.AnyAsync(u => u.Userid == leaving.Id)));
    }

    [Fact]
    public async Task RemoveUserAsync_AsNonAdmin_ThrowsPermissionDenied_AndKeepsMembership()
    {
        var admin = await Data.CreateAdminAsync();
        var member = await Data.CreateContributorAsync();
        var project = await Data.CreateProjectAsync(admin, "Project", member);

        await Assert.ThrowsAsync<PermissionDeniedException>(() =>
            Service.RemoveUserAsync(
                member.Id,
                project.Projectid,
                clientIsAdmin: false,
                CancellationToken.None
            )
        );

        Assert.True(await IsMemberAsync(project.Projectid, member.Id));
    }

    // CanAccessAsync

    [Fact]
    public async Task CanAccessAsync_AllowsAdminsAndMembers_ButNotOutsiders()
    {
        var admin = await Data.CreateAdminAsync();
        var member = await Data.CreateContributorAsync();
        var outsider = await Data.CreateContributorAsync();
        var project = await Data.CreateProjectAsync(admin, "Project", member);
        var id = project.Projectid;

        Assert.True(await Service.CanAccessAsync(id, admin.Id, isAdmin: true, default));
        Assert.True(await Service.CanAccessAsync(id, member.Id, isAdmin: false, default));
        Assert.False(await Service.CanAccessAsync(id, outsider.Id, isAdmin: false, default));
    }

    private Task<bool> IsMemberAsync(int projectId, int userId) =>
        Data.WithDbAsync(db =>
            db.Projectusers.AnyAsync(pu => pu.Projectid == projectId && pu.Userid == userId)
        );
}
