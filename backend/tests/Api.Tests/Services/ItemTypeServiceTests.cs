using Api.Dtos;
using Api.Exceptions;
using Api.Tests.Support;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Services;

public class ItemTypeServiceTests
{
    private const int MissingId = 9999;

    private readonly TestDb db = new();

    // ---------------------------------------------------------------- ListAsync

    [Fact]
    public async Task ListAsync_ReturnsAllItemTypesOrderedByName()
    {
        db.AddItemType("Task");
        db.AddItemType("Bug");
        db.AddItemType("Epic", isActive: false);

        var types = (await db.CreateItemTypeService().ListAsync()).ToList();

        Assert.Equal(new[] { "Bug", "Epic", "Task" }, types.Select(t => t.Name));
        Assert.All(types, type => Assert.True(type.Id > 0));
    }

    [Fact]
    public async Task ListAsync_ActiveOnly_SkipsInactiveItemTypes()
    {
        db.AddItemType("Bug");
        db.AddItemType("Legacy", isActive: false);

        var activeOnly = (await db.CreateItemTypeService().ListAsync(activeOnly: true)).ToList();

        var only = Assert.Single(activeOnly);
        Assert.Equal("Bug", only.Name);
        Assert.True(only.IsActive);
    }

    [Fact]
    public async Task ListAsync_NoItemTypes_ReturnsEmpty()
    {
        Assert.Empty(await db.CreateItemTypeService().ListAsync());
    }

    // -------------------------------------------------------------- CreateAsync

    [Fact]
    public async Task CreateAsync_ValidRequest_SavesActiveItemTypeAndReturnsDto()
    {
        var created = await db.CreateItemTypeService()
            .CreateAsync(new CreateItemTypeRequest("Bug"));

        Assert.True(created.Id > 0);
        Assert.Equal("Bug", created.Name);
        Assert.True(created.IsActive);

        using var context = db.CreateContext();
        var saved = await context.Itemtypes.SingleAsync();
        Assert.Equal(created.Id, saved.Itemtypeid);
        Assert.Equal("Bug", saved.Name);
        Assert.True(saved.Isactive);
    }

    [Fact]
    public async Task CreateAsync_NameWithSurroundingSpaces_IsTrimmed()
    {
        var created = await db.CreateItemTypeService()
            .CreateAsync(new CreateItemTypeRequest("  Bug \n"));

        Assert.Equal("Bug", created.Name);

        using var context = db.CreateContext();
        Assert.Equal("Bug", (await context.Itemtypes.SingleAsync()).Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_EmptyName_ThrowsValidationExceptionAndSavesNothing(string name)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateItemTypeService().CreateAsync(new CreateItemTypeRequest(name))
        );

        using var context = db.CreateContext();
        Assert.Empty(context.Itemtypes);
    }

    [Theory]
    [InlineData("Bug")]
    [InlineData("bug")]
    [InlineData("  BUG  ")]
    public async Task CreateAsync_DuplicateNameIgnoringCaseAndSpaces_ThrowsValidationException(
        string name
    )
    {
        db.AddItemType("Bug");

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateItemTypeService().CreateAsync(new CreateItemTypeRequest(name))
        );

        Assert.Contains($"An item type named '{name.Trim()}' already exists.", exception.Message);

        using var context = db.CreateContext();
        Assert.Single(context.Itemtypes);
    }

    [Fact]
    public async Task CreateAsync_DuplicateOfAnInactiveItemType_ThrowsValidationException()
    {
        db.AddItemType("Legacy", isActive: false);

        await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateItemTypeService().CreateAsync(new CreateItemTypeRequest("legacy"))
        );
    }

    // -------------------------------------------------------------- UpdateAsync

    [Fact]
    public async Task UpdateAsync_ValidRequest_SavesNameAndActiveFlag()
    {
        var id = db.AddItemType("Bug");

        var updated = await db.CreateItemTypeService()
            .UpdateAsync(new UpdateItemTypeRequest(id, "Defect", IsActive: false));

        Assert.Equal(id, updated.Id);
        Assert.Equal("Defect", updated.Name);
        Assert.False(updated.IsActive);

        using var context = db.CreateContext();
        var saved = await context.Itemtypes.SingleAsync();
        Assert.Equal("Defect", saved.Name);
        Assert.False(saved.Isactive);
    }

    [Fact]
    public async Task UpdateAsync_NameWithSurroundingSpaces_IsTrimmed()
    {
        var id = db.AddItemType("Bug");

        var updated = await db.CreateItemTypeService()
            .UpdateAsync(new UpdateItemTypeRequest(id, "  Defect  ", IsActive: true));

        Assert.Equal("Defect", updated.Name);
    }

    [Fact]
    public async Task UpdateAsync_KeepingItsOwnNameInADifferentCase_IsAllowed()
    {
        var id = db.AddItemType("Bug");

        var updated = await db.CreateItemTypeService()
            .UpdateAsync(new UpdateItemTypeRequest(id, "BUG", IsActive: true));

        Assert.Equal("BUG", updated.Name);
    }

    [Fact]
    public async Task UpdateAsync_MissingItemType_ThrowsKeyNotFound()
    {
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            db.CreateItemTypeService()
                .UpdateAsync(new UpdateItemTypeRequest(MissingId, "Bug", IsActive: true))
        );

        Assert.Equal($"Item type with ID '{MissingId}' was not found.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_NameTakenByAnotherItemType_ThrowsAndLeavesItUnchanged()
    {
        var bugId = db.AddItemType("Bug");
        db.AddItemType("Task");

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateItemTypeService()
                .UpdateAsync(new UpdateItemTypeRequest(bugId, "task", IsActive: false))
        );

        Assert.Contains("An item type named 'task' already exists.", exception.Message);

        using var context = db.CreateContext();
        var unchanged = await context.Itemtypes.SingleAsync(t => t.Itemtypeid == bugId);
        Assert.Equal("Bug", unchanged.Name);
        Assert.True(unchanged.Isactive);
    }

    // ----------------------------------------------------- EnsureAssignableAsync

    [Fact]
    public async Task EnsureAssignableAsync_ActiveItemType_DoesNotThrow()
    {
        var id = db.AddItemType("Bug");

        await db.CreateItemTypeService().EnsureAssignableAsync(id, currentItemTypeId: null);
    }

    [Fact]
    public async Task EnsureAssignableAsync_InactiveItemType_ThrowsInactiveEntityException()
    {
        var id = db.AddItemType("Legacy", isActive: false);

        var exception = await Assert.ThrowsAsync<InactiveEntityException>(() =>
            db.CreateItemTypeService().EnsureAssignableAsync(id, currentItemTypeId: null)
        );

        Assert.Equal("Item type 'Legacy' is inactive and cannot be assigned.", exception.Message);
    }

    [Fact]
    public async Task EnsureAssignableAsync_MissingItemType_ThrowsKeyNotFound()
    {
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            db.CreateItemTypeService().EnsureAssignableAsync(MissingId, currentItemTypeId: null)
        );

        Assert.Equal($"Item type '{MissingId}' was not found.", exception.Message);
    }

    [Fact]
    public async Task EnsureAssignableAsync_UnchangedInactiveItemType_IsLeftAlone()
    {
        var id = db.AddItemType("Legacy", isActive: false);

        await db.CreateItemTypeService().EnsureAssignableAsync(id, currentItemTypeId: id);
    }

    [Fact]
    public async Task EnsureAssignableAsync_UnchangedMissingItemType_IsNotLookedUp()
    {
        await db.CreateItemTypeService()
            .EnsureAssignableAsync(MissingId, currentItemTypeId: MissingId);
    }
}
