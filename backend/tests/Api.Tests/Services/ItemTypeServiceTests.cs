using Api.Data;
using Api.Dtos;
using Api.Models;
using Api.Services;
using Api.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Services;

public class ItemTypeServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static ItemTypeService CreateService(AppDbContext db)
    {
        return new ItemTypeService(
            db,
            new CreateItemTypeRequestValidator(),
            new UpdateItemTypeRequestValidator()
        );
    }

    [Fact]
    public async Task CreateAsync_CreatesType_AndAppearsInActiveList()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateItemTypeRequest("Bug"));
        var activeTypes = await service.ListAsync(activeOnly: true);

        Assert.Equal("Bug", created.Name);
        Assert.True(created.IsActive);
        Assert.Contains(activeTypes, t => t.Name == "Bug");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_CaseInsensitive_ThrowsValidationException()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await service.CreateAsync(new CreateItemTypeRequest("Task"));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateItemTypeRequest("task"))
        );
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ThrowsValidationException()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateItemTypeRequest(""))
        );
    }

    [Fact]
    public async Task UpdateAsync_Deactivating_RemovesFromActiveListOnly()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateItemTypeRequest("Feature"));
        await service.UpdateAsync(new UpdateItemTypeRequest(created.Id, "Feature", false));

        var activeOnly = await service.ListAsync(activeOnly: true);
        var all = await service.ListAsync(activeOnly: false);

        Assert.DoesNotContain(activeOnly, t => t.Id == created.Id);
        Assert.Contains(all, t => t.Id == created.Id);
    }

    [Fact]
    public async Task EnsureAssignableAsync_InactiveType_AndNullCurrent_ThrowsValidationException()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateItemTypeRequest("OldType"));
        await service.UpdateAsync(new UpdateItemTypeRequest(created.Id, "OldType", false));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.EnsureAssignableAsync(created.Id, currentItemTypeId: null)
        );
    }

    [Fact]
    public async Task EnsureAssignableAsync_InactiveType_MatchesCurrentItemTypeId_Passes()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateItemTypeRequest("Legacy"));
        await service.UpdateAsync(new UpdateItemTypeRequest(created.Id, "Legacy", false));

        await service.EnsureAssignableAsync(created.Id, currentItemTypeId: created.Id);
    }

    [Fact]
    public async Task EnsureAssignableAsync_DifferentInactiveType_OnEdit_ThrowsValidationException()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var type1 = await service.CreateAsync(new CreateItemTypeRequest("Type1"));
        var type2 = await service.CreateAsync(new CreateItemTypeRequest("Type2"));
        await service.UpdateAsync(new UpdateItemTypeRequest(type2.Id, "Type2", false));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.EnsureAssignableAsync(type2.Id, currentItemTypeId: type1.Id)
        );
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateAsync(new UpdateItemTypeRequest(999, "NonExistent", true))
        );
    }
}
