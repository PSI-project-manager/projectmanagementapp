using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Api.Models;
using Api.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Integration;

public class ItemsControllerTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    private TestUser admin = null!;
    private TestUser member = null!;
    private TestUser outsider = null!;
    private Project project = null!;
    private Itemtype bug = null!;
    private Status todo = null!;

    // every test starts with one project, a member, an outsider, an item type and a status
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        admin = await Data.CreateAdminAsync();
        member = await Data.CreateContributorAsync();
        outsider = await Data.CreateContributorAsync();
        project = await Data.CreateProjectAsync(admin, "Project", member);
        bug = await Data.CreateItemTypeAsync("Bug");
        todo = await Data.CreateStatusAsync("To Do");
    }

    private CreateItemRequest ValidRequest(
        string title = "Fix login",
        int? itemTypeId = null,
        int? statusId = null,
        int? projectId = null
    ) =>
        new(
            title,
            "Steps to reproduce",
            projectId ?? project.Projectid,
            itemTypeId ?? bug.Itemtypeid,
            statusId ?? todo.Statusid
        );

    private Task<int> ItemCountAsync() => Data.WithDbAsync(db => db.Items.CountAsync());

    // auth

    [Fact]
    public async Task Endpoints_WithoutToken_Return401()
    {
        var client = AnonymousClient();

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.GetAsync($"/api/items?projectId={project.Projectid}")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/items", ValidRequest())).StatusCode
        );
    }

    [Fact]
    public async Task Endpoints_ForUserWithNoRole_Return403_EvenIfTheyAreAMember()
    {
        var noRole = await Data.CreateUserAsync([]);
        await Data.WithDbAsync(db =>
        {
            db.Projectusers.Add(
                new Projectuser { Projectid = project.Projectid, Userid = noRole.Id }
            );
            return db.SaveChangesAsync();
        });
        var client = ClientFor(noRole);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.GetAsync($"/api/items?projectId={project.Projectid}")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync("/api/items", ValidRequest())).StatusCode
        );
        Assert.Equal(0, await ItemCountAsync());
    }

    // GET /api/items?projectId=

    [Fact]
    public async Task List_AsMember_ReturnsProjectItemsNewestFirst_WithTypeAndStatusNames()
    {
        var otherProject = await Data.CreateProjectAsync(admin, "Other", outsider);
        var first = await Data.CreateItemAsync(
            project.Projectid,
            bug.Itemtypeid,
            todo.Statusid,
            "First"
        );
        var second = await Data.CreateItemAsync(
            project.Projectid,
            bug.Itemtypeid,
            todo.Statusid,
            "Second"
        );
        await Data.CreateItemAsync(
            otherProject.Projectid,
            bug.Itemtypeid,
            todo.Statusid,
            "Elsewhere"
        );

        var items = await ClientFor(member)
            .GetFromJsonAsync<List<ItemDto>>($"/api/items?projectId={project.Projectid}");

        Assert.NotNull(items);
        Assert.Equal([second.Itemid, first.Itemid], items.Select(i => i.Id));
        Assert.All(
            items,
            i =>
            {
                Assert.Equal(project.Projectid, i.ProjectId);
                Assert.Equal("Bug", i.ItemTypeName);
                Assert.Equal("To Do", i.StatusName);
            }
        );
    }

    [Fact]
    public async Task List_AsNonMember_Returns404_SoProjectIdsDoNotLeak()
    {
        await Data.CreateItemAsync(project.Projectid, bug.Itemtypeid, todo.Statusid, "Secret");

        var response = await ClientFor(outsider)
            .GetAsync($"/api/items?projectId={project.Projectid}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            $"Project '{project.Projectid}' was not found.",
            (await ReadProblemAsync(response)).Detail
        );
    }

    [Fact]
    public async Task List_ForMissingProject_Returns404()
    {
        var response = await ClientFor(admin).GetAsync("/api/items?projectId=999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // POST /api/items

    [Fact]
    public async Task Create_AsMember_Returns201_WithLocation_AndPersistsTrimmedItem()
    {
        var response = await ClientFor(member)
            .PostAsJsonAsync(
                "/api/items",
                new CreateItemRequest(
                    "  Fix login  ",
                    "  Steps  ",
                    project.Projectid,
                    bug.Itemtypeid,
                    todo.Statusid
                )
            );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ItemDto>())!;
        Assert.Equal($"/api/items/{created.Id}", response.Headers.Location!.OriginalString);
        Assert.Equal("Fix login", created.Title);
        Assert.Equal("Steps", created.Description);
        Assert.Equal(project.Projectid, created.ProjectId);
        Assert.Equal(bug.Itemtypeid, created.ItemTypeId);
        Assert.Equal(todo.Statusid, created.StatusId);

        var stored = await Data.WithDbAsync(db => db.Items.SingleAsync());
        Assert.Equal(created.Id, stored.Itemid);
        Assert.Equal("Fix login", stored.Title);
        Assert.Equal("Steps", stored.Description);
        Assert.Null(stored.Assignedtouserid);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_Returns400WithEveryMessage_AndSavesNothing()
    {
        var response = await ClientFor(member)
            .PostAsJsonAsync("/api/items", new CreateItemRequest("", "", 0, 0, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Contains("Title is required.", problem.Detail);
        Assert.Contains("Description is required.", problem.Detail);
        Assert.Contains("Project is required.", problem.Detail);
        Assert.Contains("Item type is required.", problem.Detail);
        Assert.Contains("Status is required.", problem.Detail);
        Assert.Equal(0, await ItemCountAsync());
    }

    [Fact]
    public async Task Create_InProjectTheUserIsNotIn_Returns404_AndSavesNothing()
    {
        var response = await ClientFor(outsider).PostAsJsonAsync("/api/items", ValidRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await ItemCountAsync());
    }

    [Fact]
    public async Task Create_InMissingProject_Returns404()
    {
        var response = await ClientFor(admin)
            .PostAsJsonAsync("/api/items", ValidRequest(projectId: 999));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithMissingItemType_Returns404()
    {
        var response = await ClientFor(member)
            .PostAsJsonAsync("/api/items", ValidRequest(itemTypeId: 999));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Item type '999' was not found.", (await ReadProblemAsync(response)).Detail);
    }

    [Fact]
    public async Task Create_WithInactiveItemType_Returns409()
    {
        var retired = await Data.CreateItemTypeAsync("Epic", isActive: false);

        var response = await ClientFor(member)
            .PostAsJsonAsync("/api/items", ValidRequest(itemTypeId: retired.Itemtypeid));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal("Entity is inactive", problem.Title);
        Assert.Equal("Item type 'Epic' is inactive and cannot be assigned.", problem.Detail);
        Assert.Equal(0, await ItemCountAsync());
    }
}
