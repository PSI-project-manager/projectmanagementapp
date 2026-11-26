using Api.Dtos;
using Api.Exceptions;
using Api.Tests.Support;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Services;

public class StatusServiceTests
{
    private const int MissingId = 9999;

    private readonly TestDb db = new();

    // ---------------------------------------------------------------- ListAsync

    [Fact]
    public async Task ListAsync_ReturnsAllStatusesOrderedBySortOrderThenName()
    {
        db.AddStatus("Done", sortOrder: 3);
        db.AddStatus("In progress", sortOrder: 2);
        db.AddStatus("Blocked", sortOrder: 2);
        db.AddStatus("To Do", sortOrder: 1);

        var statuses = (await db.CreateStatusService().ListAsync()).ToList();

        Assert.Equal(
            new[] { "To Do", "Blocked", "In progress", "Done" },
            statuses.Select(s => s.Name)
        );
        Assert.Equal(new[] { 1, 2, 2, 3 }, statuses.Select(s => s.SortOrder));
    }

    [Fact]
    public async Task ListAsync_ActiveOnly_SkipsInactiveStatuses()
    {
        db.AddStatus("To Do");
        db.AddStatus("Closed", isActive: false, sortOrder: 2);

        var activeOnly = (await db.CreateStatusService().ListAsync(activeOnly: true)).ToList();

        var only = Assert.Single(activeOnly);
        Assert.Equal("To Do", only.Name);
        Assert.True(only.IsActive);
    }

    [Fact]
    public async Task ListAsync_NoStatuses_ReturnsEmpty()
    {
        Assert.Empty(await db.CreateStatusService().ListAsync());
    }

    // -------------------------------------------------------------- CreateAsync

    [Fact]
    public async Task CreateAsync_ValidRequest_SavesActiveStatusAndReturnsDto()
    {
        var created = await db.CreateStatusService()
            .CreateAsync(new CreateStatusRequest("To Do", SortOrder: 5));

        Assert.True(created.Id > 0);
        Assert.Equal("To Do", created.Name);
        Assert.Equal(5, created.SortOrder);
        Assert.True(created.IsActive);

        using var context = db.CreateContext();
        var saved = await context.Statuses.SingleAsync();
        Assert.Equal(created.Id, saved.Statusid);
        Assert.Equal("To Do", saved.Name);
        Assert.Equal(5, saved.Sortorder);
        Assert.True(saved.Isactive);
    }

    [Fact]
    public async Task CreateAsync_NoSortOrderAndNoStatusesYet_StartsAtOne()
    {
        var created = await db.CreateStatusService().CreateAsync(new CreateStatusRequest("To Do"));

        Assert.Equal(1, created.SortOrder);
    }

    [Fact]
    public async Task CreateAsync_NoSortOrder_GoesAfterTheHighestExistingOne()
    {
        db.AddStatus("To Do", sortOrder: 3);
        db.AddStatus("Done", sortOrder: 7);

        var created = await db.CreateStatusService()
            .CreateAsync(new CreateStatusRequest("Blocked"));

        Assert.Equal(8, created.SortOrder);
    }

    [Fact]
    public async Task CreateAsync_SortOrderZero_IsAccepted()
    {
        var created = await db.CreateStatusService()
            .CreateAsync(new CreateStatusRequest("To Do", SortOrder: 0));

        Assert.Equal(0, created.SortOrder);
    }

    [Fact]
    public async Task CreateAsync_NegativeSortOrder_ThrowsValidationExceptionAndSavesNothing()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateStatusService().CreateAsync(new CreateStatusRequest("To Do", SortOrder: -1))
        );

        using var context = db.CreateContext();
        Assert.Empty(context.Statuses);
    }

    [Fact]
    public async Task CreateAsync_NameWithSurroundingSpaces_IsTrimmed()
    {
        var created = await db.CreateStatusService()
            .CreateAsync(new CreateStatusRequest("  To Do \n"));

        Assert.Equal("To Do", created.Name);

        using var context = db.CreateContext();
        Assert.Equal("To Do", (await context.Statuses.SingleAsync()).Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_EmptyName_ThrowsValidationExceptionAndSavesNothing(string name)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateStatusService().CreateAsync(new CreateStatusRequest(name))
        );

        using var context = db.CreateContext();
        Assert.Empty(context.Statuses);
    }

    [Theory]
    [InlineData("To Do")]
    [InlineData("to do")]
    [InlineData("  TO DO  ")]
    public async Task CreateAsync_DuplicateNameIgnoringCaseAndSpaces_ThrowsValidationException(
        string name
    )
    {
        db.AddStatus("To Do");

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateStatusService().CreateAsync(new CreateStatusRequest(name))
        );

        Assert.Contains($"A status named '{name.Trim()}' already exists.", exception.Message);

        using var context = db.CreateContext();
        Assert.Single(context.Statuses);
    }

    [Fact]
    public async Task CreateAsync_DuplicateOfAnInactiveStatus_ThrowsValidationException()
    {
        db.AddStatus("Closed", isActive: false);

        await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateStatusService().CreateAsync(new CreateStatusRequest("closed"))
        );
    }

    // -------------------------------------------------------------- UpdateAsync

    [Fact]
    public async Task UpdateAsync_ValidRequest_SavesNameSortOrderAndActiveFlag()
    {
        var id = db.AddStatus("To Do");

        var updated = await db.CreateStatusService()
            .UpdateAsync(new UpdateStatusRequest(id, "Backlog", SortOrder: 4, IsActive: false));

        Assert.Equal(id, updated.Id);
        Assert.Equal("Backlog", updated.Name);
        Assert.Equal(4, updated.SortOrder);
        Assert.False(updated.IsActive);

        using var context = db.CreateContext();
        var saved = await context.Statuses.SingleAsync();
        Assert.Equal("Backlog", saved.Name);
        Assert.Equal(4, saved.Sortorder);
        Assert.False(saved.Isactive);
    }

    [Fact]
    public async Task UpdateAsync_NameWithSurroundingSpaces_IsTrimmed()
    {
        var id = db.AddStatus("To Do");

        var updated = await db.CreateStatusService()
            .UpdateAsync(new UpdateStatusRequest(id, "  Backlog  ", SortOrder: 1, IsActive: true));

        Assert.Equal("Backlog", updated.Name);
    }

    [Fact]
    public async Task UpdateAsync_KeepingItsOwnNameInADifferentCase_IsAllowed()
    {
        var id = db.AddStatus("To Do");

        var updated = await db.CreateStatusService()
            .UpdateAsync(new UpdateStatusRequest(id, "TO DO", SortOrder: 1, IsActive: true));

        Assert.Equal("TO DO", updated.Name);
    }

    [Fact]
    public async Task UpdateAsync_MissingStatus_ThrowsKeyNotFound()
    {
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            db.CreateStatusService()
                .UpdateAsync(new UpdateStatusRequest(MissingId, "To Do", 1, IsActive: true))
        );

        Assert.Equal($"Status with ID '{MissingId}' was not found.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_NameTakenByAnotherStatus_ThrowsAndLeavesItUnchanged()
    {
        var todoId = db.AddStatus("To Do", sortOrder: 1);
        db.AddStatus("Done", sortOrder: 2);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateStatusService()
                .UpdateAsync(new UpdateStatusRequest(todoId, "done", SortOrder: 9, IsActive: false))
        );

        Assert.Contains("A status named 'done' already exists.", exception.Message);

        using var context = db.CreateContext();
        var unchanged = await context.Statuses.SingleAsync(s => s.Statusid == todoId);
        Assert.Equal("To Do", unchanged.Name);
        Assert.Equal(1, unchanged.Sortorder);
        Assert.True(unchanged.Isactive);
    }

    // ----------------------------------------------------- EnsureAssignableAsync

    [Fact]
    public async Task EnsureAssignableAsync_ActiveStatus_DoesNotThrow()
    {
        var id = db.AddStatus("To Do");

        await db.CreateStatusService().EnsureAssignableAsync(id, currentStatusId: null);
    }

    [Fact]
    public async Task EnsureAssignableAsync_InactiveStatus_ThrowsInactiveEntityException()
    {
        var id = db.AddStatus("Closed", isActive: false);

        var exception = await Assert.ThrowsAsync<InactiveEntityException>(() =>
            db.CreateStatusService().EnsureAssignableAsync(id, currentStatusId: null)
        );

        Assert.Equal("Status 'Closed' is inactive and cannot be assigned.", exception.Message);
    }

    [Fact]
    public async Task EnsureAssignableAsync_MissingStatus_ThrowsKeyNotFound()
    {
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            db.CreateStatusService().EnsureAssignableAsync(MissingId, currentStatusId: null)
        );

        Assert.Equal($"Status '{MissingId}' was not found.", exception.Message);
    }

    [Fact]
    public async Task EnsureAssignableAsync_UnchangedInactiveStatus_IsLeftAlone()
    {
        var id = db.AddStatus("Closed", isActive: false);

        await db.CreateStatusService().EnsureAssignableAsync(id, currentStatusId: id);
    }

    [Fact]
    public async Task EnsureAssignableAsync_UnchangedMissingStatus_IsNotLookedUp()
    {
        await db.CreateStatusService().EnsureAssignableAsync(MissingId, currentStatusId: MissingId);
    }
}
