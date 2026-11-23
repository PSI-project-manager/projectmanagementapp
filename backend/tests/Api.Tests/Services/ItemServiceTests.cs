using Api.Dtos;
using Api.Exceptions;
using Api.Tests.Support;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Services;

public class ItemServiceTests
{
    private const int MissingId = 9999;
    private const string BugTypeName = "Bug";
    private const string TodoStatusName = "To Do";

    private readonly TestDb db = new();
    private readonly int adminId;
    private readonly int memberId;
    private readonly int outsiderId;
    private readonly int projectId;
    private readonly int otherProjectId;
    private readonly int bugTypeId;
    private readonly int inactiveTypeId;
    private readonly int todoStatusId;
    private readonly int inactiveStatusId;

    // xUnit builds a new instance for every test, so every test starts from this fresh data:
    // admin (not a member of anything), member (in "Website"), outsider (in no project).
    public ItemServiceTests()
    {
        adminId = db.AddUser("admin@test.local");
        memberId = db.AddUser("member@test.local");
        outsiderId = db.AddUser("outsider@test.local");

        projectId = db.AddProject("Website", adminId, memberId);
        otherProjectId = db.AddProject("Mobile app", adminId);

        bugTypeId = db.AddItemType(BugTypeName);
        inactiveTypeId = db.AddItemType("Legacy", isActive: false);
        todoStatusId = db.AddStatus(TodoStatusName);
        inactiveStatusId = db.AddStatus("Closed", isActive: false);
    }

    private CreateItemRequest ValidRequest(
        string title = "Fix login",
        string description = "Users cannot log in"
    ) => new(title, description, projectId, bugTypeId, todoStatusId);

    // ---------------------------------------------------------------- CreateAsync

    [Fact]
    public async Task CreateAsync_ValidRequest_SavesItemAndReturnsDto()
    {
        var created = await db.CreateItemService()
            .CreateAsync(ValidRequest(), currentUserId: memberId, currentUserIsAdmin: false);

        Assert.True(created.Id > 0);
        Assert.Equal("Fix login", created.Title);
        Assert.Equal("Users cannot log in", created.Description);
        Assert.Equal(projectId, created.ProjectId);
        Assert.Equal(bugTypeId, created.ItemTypeId);
        Assert.Equal(todoStatusId, created.StatusId);

        using var context = db.CreateContext();
        var saved = await context.Items.SingleAsync();
        Assert.Equal(created.Id, saved.Itemid);
        Assert.Equal("Fix login", saved.Title);
        Assert.Equal(projectId, saved.Projectid);
    }

    [Fact]
    public async Task CreateAsync_TitleAndDescriptionWithSpaces_AreTrimmed()
    {
        var created = await db.CreateItemService()
            .CreateAsync(
                ValidRequest(title: "  Fix login  ", description: "  Users cannot log in \n"),
                currentUserId: memberId,
                currentUserIsAdmin: false
            );

        Assert.Equal("Fix login", created.Title);
        Assert.Equal("Users cannot log in", created.Description);
    }

    [Fact]
    public async Task CreateAsync_AdminWhoIsNotAProjectMember_CanCreate()
    {
        var created = await db.CreateItemService()
            .CreateAsync(ValidRequest(), currentUserId: adminId, currentUserIsAdmin: true);

        Assert.True(created.Id > 0);
    }

    [Fact]
    public async Task CreateAsync_UserWithoutProjectAccess_ThrowsKeyNotFoundAndSavesNothing()
    {
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            db.CreateItemService()
                .CreateAsync(ValidRequest(), currentUserId: outsiderId, currentUserIsAdmin: false)
        );

        Assert.Equal($"Project '{projectId}' was not found.", exception.Message);
        using var context = db.CreateContext();
        Assert.Empty(context.Items);
    }

    [Fact]
    public async Task CreateAsync_MissingProject_ThrowsKeyNotFound()
    {
        var request = ValidRequest() with { ProjectId = MissingId };

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            db.CreateItemService()
                .CreateAsync(request, currentUserId: adminId, currentUserIsAdmin: true)
        );

        Assert.Equal($"Project '{MissingId}' was not found.", exception.Message);
    }

    [Theory]
    [InlineData("", "Users cannot log in")]
    [InlineData("   ", "Users cannot log in")]
    [InlineData("Fix login", "")]
    [InlineData("Fix login", "   ")]
    public async Task CreateAsync_EmptyTitleOrDescription_ThrowsValidationExceptionAndSavesNothing(
        string title,
        string description
    )
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateItemService()
                .CreateAsync(
                    ValidRequest(title, description),
                    currentUserId: memberId,
                    currentUserIsAdmin: false
                )
        );

        using var context = db.CreateContext();
        Assert.Empty(context.Items);
    }

    [Fact]
    public async Task CreateAsync_TitleLongerThan200Characters_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateItemService()
                .CreateAsync(
                    ValidRequest(title: new string('t', 201)),
                    currentUserId: memberId,
                    currentUserIsAdmin: false
                )
        );
    }

    [Fact]
    public async Task CreateAsync_NonPositiveIds_ThrowValidationException()
    {
        var service = db.CreateItemService();
        var requests = new[]
        {
            ValidRequest() with
            {
                ProjectId = 0,
            },
            ValidRequest() with
            {
                ItemTypeId = 0,
            },
            ValidRequest() with
            {
                StatusId = 0,
            },
        };

        foreach (var request in requests)
        {
            await Assert.ThrowsAsync<ValidationException>(() =>
                service.CreateAsync(request, currentUserId: memberId, currentUserIsAdmin: false)
            );
        }
    }

    [Fact]
    public async Task CreateAsync_InactiveItemType_ThrowsInactiveEntityException()
    {
        var request = ValidRequest() with { ItemTypeId = inactiveTypeId };

        var exception = await Assert.ThrowsAsync<InactiveEntityException>(() =>
            db.CreateItemService()
                .CreateAsync(request, currentUserId: memberId, currentUserIsAdmin: false)
        );

        Assert.Equal("Item type 'Legacy' is inactive and cannot be assigned.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_MissingItemType_ThrowsKeyNotFound()
    {
        var request = ValidRequest() with { ItemTypeId = MissingId };

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            db.CreateItemService()
                .CreateAsync(request, currentUserId: memberId, currentUserIsAdmin: false)
        );

        Assert.Equal($"Item type '{MissingId}' was not found.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_InactiveStatus_ThrowsInactiveEntityException()
    {
        var request = ValidRequest() with { StatusId = inactiveStatusId };

        var exception = await Assert.ThrowsAsync<InactiveEntityException>(() =>
            db.CreateItemService()
                .CreateAsync(request, currentUserId: memberId, currentUserIsAdmin: false)
        );

        Assert.Equal("Status 'Closed' is inactive and cannot be assigned.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_MissingStatus_ThrowsKeyNotFound()
    {
        var request = ValidRequest() with { StatusId = MissingId };

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            db.CreateItemService()
                .CreateAsync(request, currentUserId: memberId, currentUserIsAdmin: false)
        );

        Assert.Equal($"Status '{MissingId}' was not found.", exception.Message);
    }

    // ------------------------------------------------- GetItemsFromProjectAsync

    [Fact]
    public async Task GetItemsFromProjectAsync_ReturnsOnlyThatProjectsItemsNewestFirstWithNames()
    {
        db.AddItem(projectId, "First", bugTypeId, todoStatusId);
        db.AddItem(projectId, "Second", bugTypeId, todoStatusId);
        db.AddItem(otherProjectId, "Elsewhere", bugTypeId, todoStatusId);
        db.AddItem(projectId, "Third", bugTypeId, todoStatusId);

        var items = await db.CreateItemService()
            .GetItemsFromProjectAsync(projectId, memberId, currentUserIsAdmin: false);

        Assert.Equal(new[] { "Third", "Second", "First" }, items.Select(i => i.Title));
        Assert.All(
            items,
            item =>
            {
                Assert.Equal(projectId, item.ProjectId);
                Assert.Equal(BugTypeName, item.ItemTypeName);
                Assert.Equal(TodoStatusName, item.StatusName);
            }
        );
    }

    [Fact]
    public async Task GetItemsFromProjectAsync_ProjectWithoutItems_ReturnsEmptyList()
    {
        var items = await db.CreateItemService()
            .GetItemsFromProjectAsync(projectId, memberId, currentUserIsAdmin: false);

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetItemsFromProjectAsync_AdminWhoIsNotAMember_SeesItems()
    {
        db.AddItem(projectId, "Visible", bugTypeId, todoStatusId);

        var items = await db.CreateItemService()
            .GetItemsFromProjectAsync(projectId, adminId, currentUserIsAdmin: true);

        Assert.Single(items);
    }

    [Fact]
    public async Task GetItemsFromProjectAsync_UserWithoutAccess_ThrowsKeyNotFound()
    {
        db.AddItem(projectId, "Secret", bugTypeId, todoStatusId);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            db.CreateItemService()
                .GetItemsFromProjectAsync(projectId, outsiderId, currentUserIsAdmin: false)
        );
    }

    [Fact]
    public async Task GetItemsFromProjectAsync_MissingProject_ThrowsKeyNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            db.CreateItemService()
                .GetItemsFromProjectAsync(MissingId, adminId, currentUserIsAdmin: true)
        );
    }
}
