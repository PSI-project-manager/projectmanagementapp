using Api.Dtos;
using Api.Models;
using Api.Services;
using Api.Tests.Integration.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Integration;

// ItemTypeService resolved from the real DI container and run against PostgreSQL.
// These cover only what the in-memory tests in Api.Tests/Services cannot: the SQL the
// service's LINQ becomes, and the real schema's constraints, defaults and collation.
// EnsureAssignableAsync is left out on purpose - it is a primary-key lookup with no
// provider-specific behaviour, and ItemsControllerTests already drives it end to end.
public class ItemTypeServiceTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    private ItemTypeService Service => Resolve<ItemTypeService>();

    [Fact]
    public async Task CreateAsync_RoundTripsThroughTheRealSchema()
    {
        var created = await Service.CreateAsync(new CreateItemTypeRequest("  Bug  "));

        // the id comes from the identity-always column, the active flag from the column default
        Assert.True(created.Id > 0);
        Assert.Equal("Bug", created.Name);
        Assert.True(created.IsActive);

        var stored = await Data.WithDbAsync(db => db.Itemtypes.SingleAsync());
        Assert.Equal(created.Id, stored.Itemtypeid);
        Assert.Equal("Bug", stored.Name);
        Assert.True(stored.Isactive);
    }

    [Fact]
    public async Task CreateAsync_NameAtTheValidatorsMaxLength_FitsTheNameColumn()
    {
        // the validator's cap and the column's varchar length have to agree, or a name the
        // validator accepts blows up on insert
        var name = new string('t', Itemtype.MaxNameLength);

        await Service.CreateAsync(new CreateItemTypeRequest(name));

        Assert.Equal(
            name,
            await Data.WithDbAsync(db => db.Itemtypes.Select(t => t.Name).SingleAsync())
        );
    }

    [Fact]
    public async Task CreateAsync_NameDifferingOnlyByCase_IsRejectedBeforeTheUniqueIndexSeesIt()
    {
        await Data.CreateItemTypeAsync("Bug");

        // itemtypes_name_key is case-sensitive, so the database would happily take "bug";
        // the service's lower(name) comparison is the only thing stopping it
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            Service.CreateAsync(new CreateItemTypeRequest("  bug  "))
        );

        Assert.Contains("An item type named 'bug' already exists.", exception.Message);
        Assert.Equal(1, await Data.WithDbAsync(db => db.Itemtypes.CountAsync()));
    }

    [Fact]
    public async Task UpdateAsync_RenamingOntoAnotherRowsNameIgnoringCase_ChangesNothing()
    {
        var bug = await Data.CreateItemTypeAsync("Bug");
        await Data.CreateItemTypeAsync("Task");

        await Assert.ThrowsAsync<ValidationException>(() =>
            Service.UpdateAsync(new UpdateItemTypeRequest(bug.Itemtypeid, "TASK", IsActive: false))
        );

        var stored = await Data.WithDbAsync(db =>
            db.Itemtypes.SingleAsync(t => t.Itemtypeid == bug.Itemtypeid)
        );
        Assert.Equal("Bug", stored.Name);
        Assert.True(stored.Isactive);
    }

    [Fact]
    public async Task UpdateAsync_RecasingItsOwnName_IsPersisted()
    {
        var bug = await Data.CreateItemTypeAsync("Bug");

        var updated = await Service.UpdateAsync(
            new UpdateItemTypeRequest(bug.Itemtypeid, "BUG", IsActive: false)
        );

        // the duplicate query has to exclude this row in SQL, and the unique index has to
        // tolerate the rename
        Assert.Equal("BUG", updated.Name);
        var stored = await Data.WithDbAsync(db => db.Itemtypes.SingleAsync());
        Assert.Equal("BUG", stored.Name);
        Assert.False(stored.Isactive);
    }

    [Fact]
    public async Task ListAsync_OrdersByNameInTheDatabase_AndFiltersInactiveInSql()
    {
        await Data.CreateItemTypeAsync("cherry");
        await Data.CreateItemTypeAsync("Banana", isActive: false);
        await Data.CreateItemTypeAsync("apple");

        // the ORDER BY runs in PostgreSQL under its collation, which is case-insensitive -
        // .NET's ordinal comparison would have put "Banana" first
        Assert.Equal(
            ["apple", "Banana", "cherry"],
            (await Service.ListAsync()).Select(t => t.Name)
        );

        Assert.Equal(
            ["apple", "cherry"],
            (await Service.ListAsync(activeOnly: true)).Select(t => t.Name)
        );
    }
}
