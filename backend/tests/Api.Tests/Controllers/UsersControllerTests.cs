using Api.Controllers;
using Api.Dtos;
using Api.Tests.Support;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Api.Tests.Controllers;

public class UsersControllerTests
{
    private const int MissingId = 9999;

    private readonly TestDb db = new();

    private UsersController CreateController() => new(db.CreateUserService());

    private static T OkValue<T>(ActionResult<T> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<T>(ok.Value);
    }

    // ---------------------------------------------------------- authorization

    [Fact]
    public void Controller_IsRestrictedToAdmins()
    {
        var authorize = Assert.Single(
            typeof(UsersController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
        );

        Assert.Equal("Admin", ((AuthorizeAttribute)authorize).Roles);
    }

    [Fact]
    public void Controller_HasNoAnonymousEndpoints()
    {
        var anonymous = typeof(UsersController)
            .GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Length > 0);

        Assert.Empty(anonymous);
    }

    // ------------------------------------------------------------------- List

    [Fact]
    public async Task List_ReturnsOkWithAllUsers()
    {
        db.AddUserWithRoles("a@test.local", "Anna", ["Admin"]);
        db.AddUserWithRoles("b@test.local", "Ben", ["Contributor"], isActive: false);

        var users = OkValue(await CreateController().List());

        Assert.Equal(new[] { "Anna", "Ben" }, users.Select(u => u.FullName));
    }

    [Fact]
    public async Task List_ActiveOnly_ReturnsOnlyActiveUsers()
    {
        db.AddUserWithRoles("a@test.local", "Anna", ["Admin"]);
        db.AddUserWithRoles("b@test.local", "Ben", ["Contributor"], isActive: false);

        var users = OkValue(await CreateController().List(activeOnly: true));

        Assert.Equal("Anna", Assert.Single(users).FullName);
    }

    [Fact]
    public async Task List_NoUsers_ReturnsOkWithEmptyList()
    {
        Assert.Empty(OkValue(await CreateController().List()));
    }

    // ----------------------------------------------------------------- Create

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedAtListWithTheNewUser()
    {
        var result = await CreateController()
            .Create(new CreateUserRequest("jane@example.com", "Jane Doe", "password123"));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(UsersController.List), created.ActionName);

        var dto = Assert.IsType<UserDto>(created.Value);
        Assert.Equal("jane@example.com", dto.Email);
        Assert.Equal("Jane Doe", dto.FullName);
        Assert.True(dto.IsActive);
        Assert.Equal(dto.Id, created.RouteValues!["id"]);
    }

    [Fact]
    public async Task Create_InvalidRequest_PropagatesValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateController().Create(new CreateUserRequest("nope", "", "x"))
        );
    }

    [Fact]
    public async Task Create_DuplicateEmail_PropagatesValidationException()
    {
        db.AddUserWithRoles("jane@example.com", "Jane", []);

        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateController()
                .Create(new CreateUserRequest("jane@example.com", "Other Jane", "password123"))
        );
    }

    // ----------------------------------------------------------------- Update

    [Fact]
    public async Task Update_UsesTheRouteIdAndBodyFields()
    {
        var id = db.AddUserWithRoles("old@test.local", "Old", ["Contributor"]);
        db.AddRole("Admin");

        var result = await CreateController()
            .Update(id, new UpdateUserRequestBody("new@test.local", "New", ["Admin"]));

        var dto = OkValue(result);
        Assert.Equal(id, dto.Id);
        Assert.Equal("new@test.local", dto.Email);
        Assert.Equal("New", dto.FullName);
        Assert.Equal(new[] { "Admin" }, dto.Roles);
        Assert.Equal(new[] { "Admin" }, db.GetRoleNames(id));
    }

    [Fact]
    public async Task Update_MissingUser_PropagatesKeyNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateController().Update(MissingId, new UpdateUserRequestBody("x@test.local", "X", []))
        );
    }

    [Fact]
    public async Task Update_UnknownRole_PropagatesKeyNotFound()
    {
        var id = db.AddUserWithRoles("user@test.local", "User", []);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateController()
                .Update(id, new UpdateUserRequestBody("user@test.local", "User", ["Ghost"]))
        );
    }

    // ------------------------------------------------------ Activate/Deactivate

    [Fact]
    public async Task Activate_ActivatesTheUserAndReturnsOk()
    {
        var id = db.AddUserWithRoles("user@test.local", "User", ["Admin"], isActive: false);

        var dto = OkValue(await CreateController().Activate(id));

        Assert.Equal(id, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(new[] { "Admin" }, dto.Roles);
    }

    [Fact]
    public async Task Deactivate_DeactivatesTheUserAndReturnsOk()
    {
        var id = db.AddUserWithRoles("user@test.local", "User", ["Admin"]);

        var dto = OkValue(await CreateController().Deactivate(id));

        Assert.Equal(id, dto.Id);
        Assert.False(dto.IsActive);
    }

    [Fact]
    public async Task Activate_MissingUser_PropagatesKeyNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateController().Activate(MissingId)
        );
    }

    [Fact]
    public async Task Deactivate_MissingUser_PropagatesKeyNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateController().Deactivate(MissingId)
        );
    }
}
