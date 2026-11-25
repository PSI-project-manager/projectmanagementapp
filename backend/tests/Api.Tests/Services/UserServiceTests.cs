using Api.Dtos;
using Api.Models;
using Api.Tests.Support;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Services;

public class UserServiceTests
{
    private const int MissingId = 9999;

    private readonly TestDb db = new();

    // xUnit builds a new instance per test, so every test starts with the two app roles and nothing else.
    public UserServiceTests()
    {
        db.AddRole("Admin");
        db.AddRole("Contributor");
    }

    // ---------------------------------------------------------------- ListAsync

    [Fact]
    public async Task ListAsync_ReturnsUsersOrderedByFullNameWithTheirRoles()
    {
        db.AddUserWithRoles("zed@test.local", "Zed", ["Contributor"]);
        db.AddUserWithRoles("amy@test.local", "Amy", ["Admin", "Contributor"]);
        db.AddUserWithRoles("bob@test.local", "Bob", []);

        var users = (await db.CreateUserService().ListAsync()).ToList();

        Assert.Equal(new[] { "Amy", "Bob", "Zed" }, users.Select(u => u.FullName));
        Assert.Equal(new[] { "Admin", "Contributor" }, users[0].Roles.Order());
        Assert.Empty(users[1].Roles);
        Assert.Equal(new[] { "Contributor" }, users[2].Roles);
    }

    [Fact]
    public async Task ListAsync_ActiveOnly_SkipsInactiveUsers()
    {
        db.AddUserWithRoles("active@test.local", "Active", ["Admin"]);
        db.AddUserWithRoles("inactive@test.local", "Inactive", ["Admin"], isActive: false);

        var all = await db.CreateUserService().ListAsync();
        var activeOnly = await db.CreateUserService().ListAsync(activeOnly: true);

        Assert.Equal(2, all.Count());
        var only = Assert.Single(activeOnly);
        Assert.Equal("active@test.local", only.Email);
        Assert.True(only.IsActive);
    }

    [Fact]
    public async Task ListAsync_NoUsers_ReturnsEmpty()
    {
        Assert.Empty(await db.CreateUserService().ListAsync());
    }

    // -------------------------------------------------------------- CreateAsync

    [Fact]
    public async Task CreateAsync_ValidRequest_SavesActiveUserWithNoRoles()
    {
        var created = await db.CreateUserService()
            .CreateAsync(new CreateUserRequest("jane@example.com", "Jane Doe", "password123"));

        Assert.True(created.Id > 0);
        Assert.Equal("jane@example.com", created.Email);
        Assert.Equal("Jane Doe", created.FullName);
        Assert.True(created.IsActive);
        Assert.Empty(created.Roles);

        using var context = db.CreateContext();
        var saved = await context.Users.SingleAsync();
        Assert.Equal(created.Id, saved.Userid);
        Assert.True(saved.Isactive);
        Assert.Empty(context.Userroles);
    }

    [Fact]
    public async Task CreateAsync_EmailIsLowerCased_AndFullNameTrimmed()
    {
        var created = await db.CreateUserService()
            .CreateAsync(new CreateUserRequest("Jane@Example.COM", "  Jane Doe  ", "password123"));

        Assert.Equal("jane@example.com", created.Email);
        Assert.Equal("Jane Doe", created.FullName);
    }

    [Fact]
    public async Task CreateAsync_StoresAHashNotThePlainPassword_ThatVerifies()
    {
        await db.CreateUserService()
            .CreateAsync(new CreateUserRequest("jane@example.com", "Jane Doe", "password123"));

        using var context = db.CreateContext();
        var saved = await context.Users.SingleAsync();

        Assert.NotEqual("password123", saved.Passwordhash);

        var hasher = new PasswordHasher<User>();
        Assert.Equal(
            PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(saved, saved.Passwordhash, "password123")
        );
        Assert.Equal(
            PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(saved, saved.Passwordhash, "something-else")
        );
    }

    [Fact]
    public async Task CreateAsync_EmailAlreadyUsed_IgnoringCase_ThrowsValidationException()
    {
        db.AddUserWithRoles("jane@example.com", "Existing Jane", []);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateUserService()
                .CreateAsync(new CreateUserRequest("JANE@example.com", "New Jane", "password123"))
        );

        Assert.Equal("A user with email 'jane@example.com' already exists.", exception.Message);
        using var context = db.CreateContext();
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Theory]
    [InlineData("not-an-email", "Jane Doe", "password123")]
    [InlineData("jane@example.com", "", "password123")]
    [InlineData("jane@example.com", "Jane Doe", "short")]
    [InlineData("", "", "")]
    public async Task CreateAsync_InvalidRequest_ThrowsValidationExceptionAndSavesNothing(
        string email,
        string fullName,
        string password
    )
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateUserService().CreateAsync(new CreateUserRequest(email, fullName, password))
        );

        using var context = db.CreateContext();
        Assert.Empty(context.Users);
    }

    // -------------------------------------------------------------- UpdateAsync

    [Fact]
    public async Task UpdateAsync_ChangesEmailAndFullName_AndReturnsDto()
    {
        var id = db.AddUserWithRoles("old@test.local", "Old Name", ["Contributor"]);

        var updated = await db.CreateUserService()
            .UpdateAsync(new UpdateUserRequest(id, "new@test.local", "New Name", ["Contributor"]));

        Assert.Equal(id, updated.Id);
        Assert.Equal("new@test.local", updated.Email);
        Assert.Equal("New Name", updated.FullName);

        using var context = db.CreateContext();
        var saved = await context.Users.SingleAsync();
        Assert.Equal("new@test.local", saved.Email);
        Assert.Equal("New Name", saved.Fullname);
    }

    [Fact]
    public async Task UpdateAsync_EmailIsLowerCased_AndFullNameTrimmed()
    {
        var id = db.AddUserWithRoles("old@test.local", "Old Name", []);

        var updated = await db.CreateUserService()
            .UpdateAsync(new UpdateUserRequest(id, "New@Test.LOCAL", "  New Name ", []));

        Assert.Equal("new@test.local", updated.Email);
        Assert.Equal("New Name", updated.FullName);
    }

    [Fact]
    public async Task UpdateAsync_KeepingTheSameEmail_IsNotADuplicate()
    {
        var id = db.AddUserWithRoles("same@test.local", "Name", []);

        var updated = await db.CreateUserService()
            .UpdateAsync(new UpdateUserRequest(id, "same@test.local", "Renamed", []));

        Assert.Equal("Renamed", updated.FullName);
    }

    [Fact]
    public async Task UpdateAsync_AddsAndRemovesRolesToMatchTheRequest()
    {
        var id = db.AddUserWithRoles("user@test.local", "User", ["Contributor"]);

        var updated = await db.CreateUserService()
            .UpdateAsync(new UpdateUserRequest(id, "user@test.local", "User", ["Admin"]));

        Assert.Equal(new[] { "Admin" }, updated.Roles);
        Assert.Equal(new[] { "Admin" }, db.GetRoleNames(id));
    }

    [Fact]
    public async Task UpdateAsync_KeepsRolesThatAreInBothSets()
    {
        var id = db.AddUserWithRoles("user@test.local", "User", ["Admin"]);

        await db.CreateUserService()
            .UpdateAsync(
                new UpdateUserRequest(id, "user@test.local", "User", ["Admin", "Contributor"])
            );

        Assert.Equal(new[] { "Admin", "Contributor" }, db.GetRoleNames(id));
    }

    [Fact]
    public async Task UpdateAsync_EmptyRoleList_RemovesAllRoles()
    {
        var id = db.AddUserWithRoles("user@test.local", "User", ["Admin", "Contributor"]);

        var updated = await db.CreateUserService()
            .UpdateAsync(new UpdateUserRequest(id, "user@test.local", "User", []));

        Assert.Empty(updated.Roles);
        Assert.Empty(db.GetRoleNames(id));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateRoleNamesInRequest_AreAssignedOnce()
    {
        var id = db.AddUserWithRoles("user@test.local", "User", []);

        var updated = await db.CreateUserService()
            .UpdateAsync(new UpdateUserRequest(id, "user@test.local", "User", ["Admin", "Admin"]));

        Assert.Equal(new[] { "Admin" }, updated.Roles);
        Assert.Equal(new[] { "Admin" }, db.GetRoleNames(id));
    }

    [Fact]
    public async Task UpdateAsync_OnlyAffectsTheTargetedUsersRoles()
    {
        var id = db.AddUserWithRoles("user@test.local", "User", ["Contributor"]);
        var otherId = db.AddUserWithRoles("other@test.local", "Other", ["Contributor"]);

        await db.CreateUserService()
            .UpdateAsync(new UpdateUserRequest(id, "user@test.local", "User", []));

        Assert.Empty(db.GetRoleNames(id));
        Assert.Equal(new[] { "Contributor" }, db.GetRoleNames(otherId));
    }

    [Fact]
    public async Task UpdateAsync_MissingUser_ThrowsKeyNotFound()
    {
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            db.CreateUserService()
                .UpdateAsync(new UpdateUserRequest(MissingId, "x@test.local", "X", []))
        );

        Assert.Equal($"User with ID '{MissingId}' was not found.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_EmailBelongsToAnotherUser_ThrowsValidationExceptionAndChangesNothing()
    {
        db.AddUserWithRoles("taken@test.local", "Taken", []);
        var id = db.AddUserWithRoles("me@test.local", "Me", []);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateUserService()
                .UpdateAsync(new UpdateUserRequest(id, "TAKEN@test.local", "Me", []))
        );

        Assert.Equal("A user with email 'taken@test.local' already exists.", exception.Message);
        using var context = db.CreateContext();
        Assert.Equal("me@test.local", (await context.Users.SingleAsync(u => u.Userid == id)).Email);
    }

    [Fact]
    public async Task UpdateAsync_UnknownRole_ThrowsKeyNotFoundAndChangesNothing()
    {
        var id = db.AddUserWithRoles("user@test.local", "User", ["Contributor"]);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            db.CreateUserService()
                .UpdateAsync(
                    new UpdateUserRequest(id, "changed@test.local", "Changed", ["Admin", "Ghost"])
                )
        );

        Assert.Equal("Role(s) 'Ghost' do not exist.", exception.Message);
        Assert.Equal(new[] { "Contributor" }, db.GetRoleNames(id));
        using var context = db.CreateContext();
        Assert.Equal("user@test.local", (await context.Users.SingleAsync()).Email);
    }

    [Fact]
    public async Task UpdateAsync_SeveralUnknownRoles_AreAllNamedInTheMessage()
    {
        var id = db.AddUserWithRoles("user@test.local", "User", []);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            db.CreateUserService()
                .UpdateAsync(new UpdateUserRequest(id, "user@test.local", "User", ["A", "B"]))
        );

        Assert.Equal("Role(s) 'A', 'B' do not exist.", exception.Message);
    }

    [Theory]
    [InlineData(0, "user@test.local", "User")]
    [InlineData(1, "not-an-email", "User")]
    [InlineData(1, "user@test.local", "")]
    public async Task UpdateAsync_InvalidRequest_ThrowsValidationException(
        int userId,
        string email,
        string fullName
    )
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            db.CreateUserService().UpdateAsync(new UpdateUserRequest(userId, email, fullName, []))
        );
    }

    // ----------------------------------------------------------- SetActiveAsync

    [Fact]
    public async Task SetActiveAsync_Deactivate_PersistsAndReturnsRoles()
    {
        var id = db.AddUserWithRoles("user@test.local", "User", ["Admin", "Contributor"]);

        var dto = await db.CreateUserService().SetActiveAsync(id, false);

        Assert.False(dto.IsActive);
        Assert.Equal(new[] { "Admin", "Contributor" }, dto.Roles.Order());
        using var context = db.CreateContext();
        Assert.False((await context.Users.SingleAsync()).Isactive);
    }

    [Fact]
    public async Task SetActiveAsync_Activate_PersistsAnInactiveUserBecomingActive()
    {
        var id = db.AddUserWithRoles("user@test.local", "User", [], isActive: false);

        var dto = await db.CreateUserService().SetActiveAsync(id, true);

        Assert.True(dto.IsActive);
        using var context = db.CreateContext();
        Assert.True((await context.Users.SingleAsync()).Isactive);
    }

    [Fact]
    public async Task SetActiveAsync_AlreadyInTheRequestedState_IsHarmless()
    {
        var id = db.AddUserWithRoles("user@test.local", "User", []);

        var dto = await db.CreateUserService().SetActiveAsync(id, true);

        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task SetActiveAsync_MissingUser_ThrowsKeyNotFound()
    {
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            db.CreateUserService().SetActiveAsync(MissingId, false)
        );

        Assert.Equal($"User with ID '{MissingId}' was not found.", exception.Message);
    }
}
