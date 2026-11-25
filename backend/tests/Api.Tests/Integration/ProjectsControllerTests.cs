using System.Net;
using System.Net.Http.Json;
using Api.Controllers;
using Api.Dtos;
using Api.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Integration;

// real HTTP requests through the whole pipeline: JWT auth, role checks, model binding,
// the exception handler and the database
public class ProjectsControllerTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    // auth

    [Fact]
    public async Task Endpoints_WithoutToken_Return401()
    {
        var client = AnonymousClient();

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/projects")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (
                await client.PostAsJsonAsync(
                    "/api/projects",
                    new CreateProjectRequestBody("P", null)
                )
            ).StatusCode
        );
    }

    [Fact]
    public async Task List_ForUserWithNoRole_Returns403()
    {
        var noRole = await Data.CreateUserAsync([]);

        var response = await ClientFor(noRole).GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // GET /api/projects

    [Fact]
    public async Task List_AsAdmin_ReturnsAllProjects()
    {
        var admin = await Data.CreateAdminAsync();
        var contributor = await Data.CreateContributorAsync();
        await Data.CreateProjectAsync(admin, "Alpha", contributor);
        await Data.CreateProjectAsync(admin, "Bravo");

        var projects = await ClientFor(admin).GetFromJsonAsync<List<ProjectDto>>("/api/projects");

        Assert.Equal(["Alpha", "Bravo"], projects!.Select(p => p.Name));
    }

    [Fact]
    public async Task List_AsContributor_ReturnsOnlyTheirProjects()
    {
        var admin = await Data.CreateAdminAsync();
        var contributor = await Data.CreateContributorAsync();
        await Data.CreateProjectAsync(admin, "Alpha", contributor);
        await Data.CreateProjectAsync(admin, "Bravo");

        var projects = await ClientFor(contributor)
            .GetFromJsonAsync<List<ProjectDto>>("/api/projects");

        Assert.Equal("Alpha", Assert.Single(projects!).Name);
    }

    // POST /api/projects

    [Fact]
    public async Task Create_AsAdmin_Returns201_WithCreatorTakenFromToken()
    {
        var admin = await Data.CreateAdminAsync();

        var response = await ClientFor(admin)
            .PostAsJsonAsync("/api/projects", new CreateProjectRequestBody(" New ", "Desc"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/projects", response.Headers.Location!.AbsolutePath);

        var project = (await response.Content.ReadFromJsonAsync<ProjectDto>())!;
        Assert.Equal("New", project.Name);
        Assert.Equal("Desc", project.Description);
        Assert.Equal(admin.Id, project.CreatedByUserId);

        // the creator is a member, so it shows up in their list even as a non-admin view
        Assert.True(
            await Data.WithDbAsync(db =>
                db.Projectusers.AnyAsync(pu => pu.Projectid == project.Id && pu.Userid == admin.Id)
            )
        );
    }

    [Fact]
    public async Task Create_AsContributor_Returns403_AndSavesNothing()
    {
        var contributor = await Data.CreateContributorAsync();

        var response = await ClientFor(contributor)
            .PostAsJsonAsync("/api/projects", new CreateProjectRequestBody("New", null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, await Data.WithDbAsync(db => db.Projects.CountAsync()));
    }

    [Fact]
    public async Task Create_WithEmptyName_Returns400ProblemDetails()
    {
        var admin = await Data.CreateAdminAsync();

        var response = await ClientFor(admin)
            .PostAsJsonAsync("/api/projects", new CreateProjectRequestBody("", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal(400, problem.Status);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Contains("Project name is required.", problem.Detail);
    }

    // PUT /api/projects/{id}

    [Fact]
    public async Task Update_AsAdmin_Returns200_AndPersists()
    {
        var admin = await Data.CreateAdminAsync();
        var project = await Data.CreateProjectAsync(admin, "Old");

        var response = await ClientFor(admin)
            .PutAsJsonAsync(
                $"/api/projects/{project.Projectid}",
                new UpdateProjectRequestBody("Renamed", "Now with a description")
            );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<ProjectDto>())!;
        Assert.Equal("Renamed", dto.Name);

        var stored = await Data.WithDbAsync(db => db.Projects.SingleAsync());
        Assert.Equal("Renamed", stored.Name);
        Assert.Equal("Now with a description", stored.Description);
    }

    [Fact]
    public async Task Update_ForMissingProject_Returns404ProblemDetails()
    {
        var admin = await Data.CreateAdminAsync();

        var response = await ClientFor(admin)
            .PutAsJsonAsync("/api/projects/999", new UpdateProjectRequestBody("Name", null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Not found", (await ReadProblemAsync(response)).Title);
    }

    [Fact]
    public async Task Update_WithTooLongName_Returns400()
    {
        var admin = await Data.CreateAdminAsync();
        var project = await Data.CreateProjectAsync(admin, "Old");

        var response = await ClientFor(admin)
            .PutAsJsonAsync(
                $"/api/projects/{project.Projectid}",
                new UpdateProjectRequestBody(
                    new string('x', Api.Models.Project.MaxNameLength + 1),
                    null
                )
            );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // GET /api/projects/{id}/users

    [Fact]
    public async Task ListUsers_AsAdmin_ReturnsMembers()
    {
        var admin = await Data.CreateAdminAsync("Admin");
        var member = await Data.CreateContributorAsync("Member");
        await Data.CreateContributorAsync("Outsider");
        var project = await Data.CreateProjectAsync(admin, "Project", member);

        var users = await ClientFor(admin)
            .GetFromJsonAsync<List<UserDto>>($"/api/projects/{project.Projectid}/users");

        var user = Assert.Single(users!);
        Assert.Equal(member.Id, user.Id);
        Assert.Equal(["Contributor"], user.Roles);
    }

    [Fact]
    public async Task ListUsers_ForMissingProject_Returns404()
    {
        var admin = await Data.CreateAdminAsync();

        var response = await ClientFor(admin).GetAsync("/api/projects/999/users");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // POST /api/projects/{id}/users

    [Fact]
    public async Task AddUser_AsAdmin_Returns204_AndAddsMembership()
    {
        var admin = await Data.CreateAdminAsync();
        var contributor = await Data.CreateContributorAsync();
        var project = await Data.CreateProjectAsync(admin, "Project");

        var response = await ClientFor(admin)
            .PostAsJsonAsync(
                $"/api/projects/{project.Projectid}/users",
                new AddProjectUserRequestBody(contributor.Id)
            );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(
            await Data.WithDbAsync(db =>
                db.Projectusers.AnyAsync(pu =>
                    pu.Projectid == project.Projectid && pu.Userid == contributor.Id
                )
            )
        );
    }

    [Fact]
    public async Task AddUser_WhoIsAlreadyAMember_Returns400()
    {
        var admin = await Data.CreateAdminAsync();
        var contributor = await Data.CreateContributorAsync();
        var project = await Data.CreateProjectAsync(admin, "Project", contributor);

        var response = await ClientFor(admin)
            .PostAsJsonAsync(
                $"/api/projects/{project.Projectid}/users",
                new AddProjectUserRequestBody(contributor.Id)
            );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "User you're trying to add is already in this project.",
            (await ReadProblemAsync(response)).Detail
        );
    }

    [Fact]
    public async Task AddUser_WhoDoesNotExist_Returns400()
    {
        var admin = await Data.CreateAdminAsync();
        var project = await Data.CreateProjectAsync(admin, "Project");

        var response = await ClientFor(admin)
            .PostAsJsonAsync(
                $"/api/projects/{project.Projectid}/users",
                new AddProjectUserRequestBody(999)
            );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddUser_ToMissingProject_Returns404()
    {
        var admin = await Data.CreateAdminAsync();
        var contributor = await Data.CreateContributorAsync();

        var response = await ClientFor(admin)
            .PostAsJsonAsync(
                "/api/projects/999/users",
                new AddProjectUserRequestBody(contributor.Id)
            );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // DELETE /api/projects/{id}/users/{userId}

    [Fact]
    public async Task RemoveUser_AsAdmin_Returns204_AndRemovesMembership()
    {
        var admin = await Data.CreateAdminAsync();
        var member = await Data.CreateContributorAsync();
        var project = await Data.CreateProjectAsync(admin, "Project", member);

        var response = await ClientFor(admin)
            .DeleteAsync($"/api/projects/{project.Projectid}/users/{member.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await Data.WithDbAsync(db => db.Projectusers.CountAsync()));
    }

    [Fact]
    public async Task RemoveUser_WhoIsNotAMember_StillReturns204()
    {
        var admin = await Data.CreateAdminAsync();
        var outsider = await Data.CreateContributorAsync();
        var project = await Data.CreateProjectAsync(admin, "Project");

        var response = await ClientFor(admin)
            .DeleteAsync($"/api/projects/{project.Projectid}/users/{outsider.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RemoveUser_FromMissingProject_Returns404()
    {
        var admin = await Data.CreateAdminAsync();

        var response = await ClientFor(admin).DeleteAsync($"/api/projects/999/users/{admin.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
