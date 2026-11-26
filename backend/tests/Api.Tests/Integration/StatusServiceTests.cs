using Api.Dtos;
using Api.Models;
using Api.Services;
using Api.Tests.Integration.Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Integration;

// StatusService resolved from the real DI container and run against PostgreSQL.
// These cover only what the in-memory tests in Api.Tests/Services cannot: the SQL the
// service's LINQ becomes, and the real schema's constraints, defaults and collation.
// EnsureAssignableAsync is left out on purpose - it is a primary-key lookup with no
// provider-specific behaviour, and ItemImportControllerTests already drives it end to end.
public class StatusServiceTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    private StatusService Service => Resolve<StatusService>();

    [Fact]
    public async Task CreateAsync_RoundTripsThroughTheRealSchema()
    {
        var created = await Service.CreateAsync(new CreateStatusRequest("  To Do  ", SortOrder: 5));

        // the id comes from the identity-always column, the active flag from the column default
        Assert.True(created.Id > 0);
        Assert.Equal("To Do", created.Name);
        Assert.Equal(5, created.SortOrder);
        Assert.True(created.IsActive);

        var stored = await Data.WithDbAsync(db => db.Statuses.SingleAsync());
        Assert.Equal(created.Id, stored.Statusid);
        Assert.Equal("To Do", stored.Name);
        Assert.Equal(5, stored.Sortorder);
        Assert.True(stored.Isactive);
    }

    [Fact]
    public async Task CreateAsync_WithoutSortOrderOnAnEmptyTable_StartsAtOne()
    {
        // MAX(sortorder) over no rows is SQL NULL; the (int?) cast is what keeps EF from
        // throwing while materialising it, and no in-memory test can prove that
        var created = await Service.CreateAsync(new CreateStatusRequest("To Do"));

        Assert.Equal(1, created.SortOrder);
        Assert.Equal(
            1,
            await Data.WithDbAsync(db => db.Statuses.Select(s => s.Sortorder).SingleAsync())
        );
    }

    [Fact]
    public async Task CreateAsync_WithoutSortOrder_ContinuesAfterTheHighestStoredOne()
    {
        await Data.CreateStatusAsync("To Do", sortOrder: 3);
        await Data.CreateStatusAsync("Done", sortOrder: 7);

        var created = await Service.CreateAsync(new CreateStatusRequest("Blocked"));

        Assert.Equal(8, created.SortOrder);
    }

    [Fact]
    public async Task CreateAsync_NameAtTheValidatorsMaxLength_FitsTheNameColumn()
    {
        // the validator's cap and the column's varchar length have to agree, or a name the
        // validator accepts blows up on insert
        var name = new string('s', Status.MaxNameLength);

        await Service.CreateAsync(new CreateStatusRequest(name));

        Assert.Equal(
            name,
            await Data.WithDbAsync(db => db.Statuses.Select(s => s.Name).SingleAsync())
        );
    }

    [Fact]
    public async Task CreateAsync_NameDifferingOnlyByCase_IsRejectedBeforeTheUniqueIndexSeesIt()
    {
        await Data.CreateStatusAsync("To Do");

        // statuses_name_key is case-sensitive, so the database would happily take "to do";
        // the service's lower(name) comparison is the only thing stopping it
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            Service.CreateAsync(new CreateStatusRequest("  to do  "))
        );

        Assert.Contains("A status named 'to do' already exists.", exception.Message);
        Assert.Equal(1, await Data.WithDbAsync(db => db.Statuses.CountAsync()));
    }

    [Fact]
    public async Task UpdateAsync_RenamingOntoAnotherRowsNameIgnoringCase_ChangesNothing()
    {
        var todo = await Data.CreateStatusAsync("To Do", sortOrder: 1);
        await Data.CreateStatusAsync("Done", sortOrder: 2);

        await Assert.ThrowsAsync<ValidationException>(() =>
            Service.UpdateAsync(
                new UpdateStatusRequest(todo.Statusid, "DONE", SortOrder: 9, IsActive: false)
            )
        );

        var stored = await Data.WithDbAsync(db =>
            db.Statuses.SingleAsync(s => s.Statusid == todo.Statusid)
        );
        Assert.Equal("To Do", stored.Name);
        Assert.Equal(1, stored.Sortorder);
        Assert.True(stored.Isactive);
    }

    [Fact]
    public async Task UpdateAsync_RecasingItsOwnName_IsPersisted()
    {
        var todo = await Data.CreateStatusAsync("To Do", sortOrder: 1);

        var updated = await Service.UpdateAsync(
            new UpdateStatusRequest(todo.Statusid, "TO DO", SortOrder: 4, IsActive: false)
        );

        // the duplicate query has to exclude this row in SQL, and the unique index has to
        // tolerate the rename
        Assert.Equal("TO DO", updated.Name);
        var stored = await Data.WithDbAsync(db => db.Statuses.SingleAsync());
        Assert.Equal("TO DO", stored.Name);
        Assert.Equal(4, stored.Sortorder);
        Assert.False(stored.Isactive);
    }

    [Fact]
    public async Task ListAsync_OrdersBySortOrderThenNameInTheDatabase_AndFiltersInactiveInSql()
    {
        await Data.CreateStatusAsync("Done", sortOrder: 3);
        await Data.CreateStatusAsync("in progress", sortOrder: 2);
        await Data.CreateStatusAsync("Blocked", sortOrder: 2, isActive: false);
        await Data.CreateStatusAsync("To Do", sortOrder: 1);

        // both ORDER BY terms run in PostgreSQL, the name tiebreak under its collation
        Assert.Equal(
            ["To Do", "Blocked", "in progress", "Done"],
            (await Service.ListAsync()).Select(s => s.Name)
        );

        Assert.Equal(
            ["To Do", "in progress", "Done"],
            (await Service.ListAsync(activeOnly: true)).Select(s => s.Name)
        );
    }
}
