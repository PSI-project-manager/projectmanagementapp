using Api.Controllers;
using Api.Dtos;
using Api.Tests.Support;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Api.Tests.Controllers;

public class AuthControllerTests
{
    private const string Email = "admin@test.local";

    private readonly TestDb db = new();

    private AuthController CreateController() => new(db.CreateAuthService());

    // ---------------------------------------------------------------- routing

    [Fact]
    public void Controller_IsRoutedAtApiAuth_AndRequiresNoAuthorization()
    {
        var route = Assert.Single(
            typeof(AuthController).GetCustomAttributes(typeof(RouteAttribute), inherit: true)
        );
        Assert.Equal("api/auth", ((RouteAttribute)route).Template);

        // login has to be reachable without a token
        Assert.Empty(
            typeof(AuthController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
        );
        Assert.Empty(
            typeof(AuthController)
                .GetMethod(nameof(AuthController.Login))!
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
        );
    }

    [Fact]
    public void Login_IsAPostAtLogin()
    {
        var post = Assert.Single(
            typeof(AuthController)
                .GetMethod(nameof(AuthController.Login))!
                .GetCustomAttributes(typeof(HttpPostAttribute), inherit: true)
        );

        Assert.Equal("login", ((HttpPostAttribute)post).Template);
    }

    // ------------------------------------------------------------------ Login

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithLoginResponse()
    {
        var id = db.AddUserWithRoles(Email, "Test Admin", ["Admin"]);

        var result = await CreateController()
            .Login(
                new LoginRequest(Email, TestDbUserExtensions.DefaultPassword),
                CancellationToken.None
            );

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<LoginResponse>(ok.Value);
        Assert.Equal(id, response.UserId);
        Assert.Equal(Email, response.Email);
        Assert.Equal("Test Admin", response.FullName);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public async Task Login_WrongPassword_PropagatesUnauthorizedAccessException()
    {
        db.AddUserWithRoles(Email, "Test Admin", ["Admin"]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateController()
                .Login(new LoginRequest(Email, "WrongPassword1!"), CancellationToken.None)
        );
    }

    [Fact]
    public async Task Login_UnknownUser_PropagatesUnauthorizedAccessException()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateController()
                .Login(
                    new LoginRequest("nobody@test.local", TestDbUserExtensions.DefaultPassword),
                    CancellationToken.None
                )
        );
    }

    [Fact]
    public async Task Login_InactiveUser_PropagatesUnauthorizedAccessException()
    {
        db.AddUserWithRoles(Email, "Test Admin", ["Admin"], isActive: false);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateController()
                .Login(
                    new LoginRequest(Email, TestDbUserExtensions.DefaultPassword),
                    CancellationToken.None
                )
        );

        Assert.Equal("User is not activated.", exception.Message);
    }

    [Fact]
    public async Task Login_InvalidRequest_PropagatesValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateController().Login(new LoginRequest("not-an-email", "x"), CancellationToken.None)
        );
    }

    [Fact]
    public async Task Login_CancelledToken_ThrowsOperationCanceled()
    {
        db.AddUserWithRoles(Email, "Test Admin", ["Admin"]);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateController()
                .Login(new LoginRequest(Email, TestDbUserExtensions.DefaultPassword), cts.Token)
        );
    }
}
